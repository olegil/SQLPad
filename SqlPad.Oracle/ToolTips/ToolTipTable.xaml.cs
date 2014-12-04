﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace SqlPad.Oracle.ToolTips
{
	public partial class ToolTipTable : IToolTip
	{
		public ToolTipTable(TableDetailsModel dataModel)
		{
			InitializeComponent();

			DataContext = dataModel;
		}

		public UserControl Control { get { return this; } }
	}

	public class TableDetailsModel : ModelBase
	{
		private int? _rowCount;
		private int? _blockCount;
		private string _compression;
		private string _organization;
		private string _clusterName;
		private string _parallelDegree;
		private string _inMemoryCompression;
		private DateTime? _lastAnalyzed;
		private int? _averageRowSize;
		private bool? _isTemporary;
		private bool? _isPartitioned;
		private long? _allocatedBytes;

		public string Title { get; set; }

		public int? RowCount
		{
			get { return _rowCount; }
			set { UpdateValueAndRaisePropertyChanged(ref _rowCount, value); }
		}

		public int? BlockCount
		{
			get { return _blockCount; }
			set { UpdateValueAndRaisePropertyChanged(ref _blockCount, value); }
		}

		public string Compression
		{
			get { return _compression; }
			set { UpdateValueAndRaisePropertyChanged(ref _compression, value); }
		}

		public string Organization
		{
			get { return _organization; }
			set { UpdateValueAndRaisePropertyChanged(ref _organization, value); }
		}

		public string ParallelDegree
		{
			get { return _parallelDegree; }
			set { UpdateValueAndRaisePropertyChanged(ref _parallelDegree, value); }
		}

		public DateTime? LastAnalyzed
		{
			get { return _lastAnalyzed; }
			set { UpdateValueAndRaisePropertyChanged(ref _lastAnalyzed, value); }
		}

		public int? AverageRowSize
		{
			get { return _averageRowSize; }
			set { UpdateValueAndRaisePropertyChanged(ref _averageRowSize, value); }
		}

		public string ClusterName
		{
			get { return _clusterName; }
			set
			{
				if (UpdateValueAndRaisePropertyChanged(ref _clusterName, value))
				{
					RaisePropertyChanged("ClusterNameVisible");
				}
			}
		}

		public bool? IsPartitioned
		{
			get { return _isPartitioned; }
			set { UpdateValueAndRaisePropertyChanged(ref _isPartitioned, value); }
		}

		public bool? IsTemporary
		{
			get { return _isTemporary; }
			set { UpdateValueAndRaisePropertyChanged(ref _isTemporary, value); }
		}

		public Visibility ClusterNameVisible
		{
			get { return String.IsNullOrEmpty(_clusterName) ? Visibility.Collapsed : Visibility.Visible; }
		}

		public long? AllocatedBytes
		{
			get { return _allocatedBytes; }
			set { UpdateValueAndRaisePropertyChanged(ref _allocatedBytes, value); }
		}

		public string InMemoryCompression
		{
			get { return _inMemoryCompression; }
			set { UpdateValueAndRaisePropertyChanged(ref _inMemoryCompression, value); }
		}

		public long? InMemoryAllocatedBytes { get; private set; }
		public long? StorageBytes { get; private set; }
		public long? NonPopulatedBytes { get; private set; }
		public string InMemoryPopulationStatus { get; private set; }

		public void SetInMemoryAllocationStatus(long? inMemoryAllocatedBytes, long? storageBytes, long? nonPopulatedBytes, string populationStatus)
		{
			InMemoryAllocatedBytes = inMemoryAllocatedBytes;
			StorageBytes = storageBytes;
			NonPopulatedBytes = nonPopulatedBytes;
			InMemoryPopulationStatus = populationStatus;

			if (!inMemoryAllocatedBytes.HasValue)
			{
				return;
			}

			RaisePropertyChanged("InMemoryAllocatedBytes");
			RaisePropertyChanged("StorageBytes");
			RaisePropertyChanged("NonPopulatedBytes");
			RaisePropertyChanged("InMemoryPopulationStatus");
			RaisePropertyChanged("InMemoryAllocationStatusVisible");
		}

		public Visibility InMemoryAllocationStatusVisible
		{
			get { return InMemoryAllocatedBytes.HasValue ? Visibility.Visible : Visibility.Collapsed; }
		}
	}
}
