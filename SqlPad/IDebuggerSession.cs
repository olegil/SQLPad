﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqlPad
{
	public interface IDebuggerSession
	{
		int? ActiveLine { get; }

		event EventHandler Attached;

		event EventHandler Detached;

		IReadOnlyList<StackTraceItem> StackTrace { get; }

		Task Start(CancellationToken cancellationToken);

		Task Continue(CancellationToken cancellationToken);

		Task StepNextLine(CancellationToken cancellationToken);

		Task StepInto(CancellationToken cancellationToken);

		Task StepOut(CancellationToken cancellationToken);

		Task Abort(CancellationToken cancellationToken);

		Task GetValue(WatchItem watchItem, CancellationToken cancellationToken);

		Task SetValue(WatchItem watchItem, CancellationToken cancellationToken);
	}

	public class StackTraceItem
	{
		public string Header { get; set; }

		public string ProgramText { get; set; }

		public int Line { get; set; }
	}
}
