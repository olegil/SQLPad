﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SqlPad
{
	public partial class OutputViewer
	{
		#region dependency properties registration
		public static readonly DependencyProperty ShowAllSessionExecutionStatisticsProperty = DependencyProperty.Register(nameof(ShowAllSessionExecutionStatistics), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(ShowAllSessionExecutionStatisticsPropertyChangedCallbackHandler));
		public static readonly DependencyProperty EnableDatabaseOutputProperty = DependencyProperty.Register(nameof(EnableDatabaseOutput), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(false));
		public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(nameof(IsPinned), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(false));
		public static readonly DependencyProperty EnableChildReferenceDataSourcesProperty = DependencyProperty.Register(nameof(EnableChildReferenceDataSources), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(false));
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(OutputViewer), new UIPropertyMetadata(TitlePropertyChangedCallbackHandler));
		public static readonly DependencyProperty DatabaseOutputProperty = DependencyProperty.Register(nameof(DatabaseOutput), typeof(string), typeof(OutputViewer), new UIPropertyMetadata(String.Empty));
		public static readonly DependencyProperty ExecutionLogProperty = DependencyProperty.Register(nameof(ExecutionLog), typeof(string), typeof(OutputViewer), new UIPropertyMetadata(String.Empty));
		public static readonly DependencyProperty ActiveResultViewerProperty = DependencyProperty.Register(nameof(ActiveResultViewer), typeof(DataGridResultViewer), typeof(OutputViewer), new UIPropertyMetadata());

		public static readonly DependencyProperty IsDebuggerControlVisibleProperty = DependencyProperty.Register(nameof(IsDebuggerControlVisible), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(false));
		public static readonly DependencyProperty IsDebuggerControlEnabledProperty = DependencyProperty.Register(nameof(IsDebuggerControlEnabled), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(true));
		public static readonly DependencyProperty BreakOnExceptionsProperty = DependencyProperty.Register(nameof(BreakOnExceptions), typeof(bool), typeof(OutputViewer), new FrameworkPropertyMetadata(BreakOnExceptionsChangedHandler));
		public static readonly DependencyProperty IsTransactionControlEnabledProperty = DependencyProperty.Register(nameof(IsTransactionControlEnabled), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(true));
		public static readonly DependencyProperty HasActiveTransactionProperty = DependencyProperty.Register(nameof(HasActiveTransaction), typeof(bool), typeof(OutputViewer), new UIPropertyMetadata(false));
		public static readonly DependencyProperty TransactionIdentifierProperty = DependencyProperty.Register(nameof(TransactionIdentifier), typeof(string), typeof(OutputViewer), new UIPropertyMetadata());
		public static readonly DependencyProperty DataOutputTypeProperty = DependencyProperty.Register(nameof(DataOutputType), typeof(DataOutputType), typeof(OutputViewer), new UIPropertyMetadata(DataOutputType.DataGrid, DataOutputTypePropertyChangedCallbackHandler));
		#endregion

		#region dependency property accessors
		[Bindable(true)]
		public DataGridResultViewer ActiveResultViewer
		{
			get { return (DataGridResultViewer)GetValue(ActiveResultViewerProperty); }
			set { SetValue(ActiveResultViewerProperty, value); }
		}

		[Bindable(true)]
		public bool ShowAllSessionExecutionStatistics
		{
			get { return (bool)GetValue(ShowAllSessionExecutionStatisticsProperty); }
			set { SetValue(ShowAllSessionExecutionStatisticsProperty, value); }
		}

		private static void ShowAllSessionExecutionStatisticsPropertyChangedCallbackHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var outputViewer = (OutputViewer)dependencyObject;
			var viewSource = (CollectionViewSource)outputViewer.TabStatistics.Resources["SortedSessionExecutionStatistics"];
			viewSource.View.Refresh();
		}

		[Bindable(true)]
		public bool HasActiveTransaction
		{
			get { return (bool)GetValue(HasActiveTransactionProperty); }
			private set { SetValue(HasActiveTransactionProperty, value); }
		}

		[Bindable(true)]
		public string TransactionIdentifier
		{
			get { return (string)GetValue(TransactionIdentifierProperty); }
			private set { SetValue(TransactionIdentifierProperty, value); }
		}

		[Bindable(true)]
		public bool IsTransactionControlEnabled
		{
			get { return (bool)GetValue(IsTransactionControlEnabledProperty); }
			private set { SetValue(IsTransactionControlEnabledProperty, value); }
		}

		[Bindable(true)]
		public bool IsDebuggerControlVisible
		{
			get { return (bool)GetValue(IsDebuggerControlVisibleProperty); }
			private set { SetValue(IsDebuggerControlVisibleProperty, value); }
		}

		[Bindable(true)]
		public bool IsDebuggerControlEnabled
		{
			get { return (bool)GetValue(IsDebuggerControlEnabledProperty); }
			private set { SetValue(IsDebuggerControlEnabledProperty, value); }
		}

		[Bindable(true)]
		public bool BreakOnExceptions
		{
			get { return (bool)GetValue(BreakOnExceptionsProperty); }
			set { SetValue(BreakOnExceptionsProperty, value); }
		}

		private static void BreakOnExceptionsChangedHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var outputViewer = (OutputViewer)dependencyObject;
			outputViewer.DocumentPage.WorkDocument.BreakOnExceptions = (bool)args.NewValue;
		}

		[Bindable(true)]
		public bool EnableDatabaseOutput
		{
			get { return (bool)GetValue(EnableDatabaseOutputProperty); }
			set { SetValue(EnableDatabaseOutputProperty, value); }
		}

		[Bindable(true)]
		public bool IsPinned
		{
			get { return (bool)GetValue(IsPinnedProperty); }
			set { SetValue(IsPinnedProperty, value); }
		}

		[Bindable(true)]
		public bool EnableChildReferenceDataSources
		{
			get { return (bool)GetValue(EnableChildReferenceDataSourcesProperty); }
			set { SetValue(EnableChildReferenceDataSourcesProperty, value); }
		}

		[Bindable(true)]
		public string DatabaseOutput
		{
			get { return (string)GetValue(DatabaseOutputProperty); }
			private set { SetValue(DatabaseOutputProperty, value); }
		}

		[Bindable(true)]
		public string ExecutionLog
		{
			get { return (string)GetValue(ExecutionLogProperty); }
			private set { SetValue(ExecutionLogProperty, value); }
		}

		[Bindable(true)]
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		private static void TitlePropertyChangedCallbackHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			((OutputViewer)dependencyObject).ConnectionAdapter.Identifier = (string)args.NewValue;
		}

		[Bindable(true)]
		public DataOutputType DataOutputType
		{
			get { return (DataOutputType)GetValue(DataOutputTypeProperty); }
			set { SetValue(DataOutputTypeProperty, value); }
		}

		private static void DataOutputTypePropertyChangedCallbackHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var outputViewer = (OutputViewer)dependencyObject;
			if (Equals(outputViewer.TabControlResult.SelectedItem, outputViewer.TabResultToFile) && (DataOutputType)args.NewValue == DataOutputType.DataGrid)
			{
				outputViewer.TabExecutionLog.IsSelected = true;
			}
			else if ((DataOutputType)args.NewValue == DataOutputType.File)
			{
				outputViewer.TabResultToFile.IsSelected = true;
			}
		}
		#endregion
	}

	public enum DataOutputType
	{
		DataGrid,
		File
	}

	public class DataOutputTypeBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (DataOutputType)value == DataOutputType.File;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (DataOutputType)System.Convert.ToInt32((bool)value);
		}
	}
}