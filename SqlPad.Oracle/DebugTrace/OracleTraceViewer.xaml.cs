﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SqlPad.Oracle.DatabaseConnection;

namespace SqlPad.Oracle.DebugTrace
{
	public partial class OracleTraceViewer : ITraceViewer
	{
		public static readonly DependencyProperty IsTracingProperty = DependencyProperty.Register("IsTracing", typeof(bool), typeof(OracleTraceViewer), new FrameworkPropertyMetadata(false));
		public static readonly DependencyProperty TraceFileNameProperty = DependencyProperty.Register("TraceFileName", typeof(string), typeof(OracleTraceViewer), new FrameworkPropertyMetadata(String.Empty));

		[Bindable(true)]
		public bool IsTracing
		{
			get { return (bool)GetValue(IsTracingProperty); }
			private set { SetValue(IsTracingProperty, value); }
		}

		[Bindable(true)]
		public string TraceFileName
		{
			get { return (string)GetValue(TraceFileNameProperty); }
			private set { SetValue(TraceFileNameProperty, value); }
		}

		private readonly OracleConnectionAdapterBase _connectionAdapter;

		private readonly ObservableCollection<OracleTraceEventModel> _traceEvents = new ObservableCollection<OracleTraceEventModel>();

		public Control Control { get { return this; } }

		public IReadOnlyCollection<OracleTraceEventModel> TraceEvents { get { return _traceEvents; } }

		public OracleTraceViewer(OracleConnectionAdapterBase connectionAdapter)
		{
			InitializeComponent();

			_connectionAdapter = connectionAdapter;

			var eventSource = OracleTraceEvent.AllTraceEvents;
			var hasDbaPrivilege = ((OracleDatabaseModelBase)connectionAdapter.DatabaseModel).HasDbaPrivilege;

			_traceEvents.AddRange(eventSource.Select(e => new OracleTraceEventModel { TraceEvent = e, IsEnabled = hasDbaPrivilege || !e.RequiresDbaPrivilege }));
		}

		private void SelectItemCommandExecutedHandler(object sender, ExecutedRoutedEventArgs e)
		{
			bool? newValue = null;
			foreach (OracleTraceEventModel traceEvent in ((ListView)sender).SelectedItems)
			{
				if (!newValue.HasValue)
				{
					newValue = !traceEvent.IsSelected;
				}

				traceEvent.IsSelected = newValue.Value;
			}
		}

		private async void TraceBottonClickHandler(object sender, RoutedEventArgs args)
		{
			try
			{
				if (IsTracing)
				{
					await _connectionAdapter.StopTraceEvents(CancellationToken.None);
				}
				else
				{
					var selectedEvents = _traceEvents.Where(e => e.IsSelected).Select(e => e.TraceEvent).ToArray();
					if (selectedEvents.Length == 0)
					{
						Messages.ShowError("Select an event first. ");
						return;
					}
					
					await _connectionAdapter.ActivateTraceEvents(selectedEvents, CancellationToken.None);
				}

				IsTracing = !IsTracing;
			}
			catch (Exception e)
			{
				Messages.ShowError(e.Message);
			}

			TraceFileName = _connectionAdapter.TraceFileName;
		}

		private void TraceFileNameHyperlinkClickHandler(object sender, RoutedEventArgs e)
		{
			var arguments = File.Exists(TraceFileName)
				? String.Format("/select,{0}", TraceFileName)
				: String.Format("/root,{0}", new FileInfo(TraceFileName).DirectoryName);

			Process.Start("explorer.exe", arguments);
		}
	}

	public class OracleTraceEventModel
	{
		public bool IsSelected { get; set; }

		public bool IsEnabled { get; set; }
		
		public OracleTraceEvent TraceEvent { get; set; }
	}
}