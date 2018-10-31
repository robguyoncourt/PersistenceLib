using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Persistence
{
	/// <summary>
	/// Takes an intitial persistence file when started log updates are turned into an observable
	/// sequence
	/// </summary>
	public class PersistenceService
	{
		private Subject<string> _fileSwitched = new Subject<string>();
		private Subject<TransactionElements> _transactionsSource = new Subject<TransactionElements>();
		private readonly Func<string, string> _getFullFilePath;
		private readonly CancellationTokenSource _tokenSource;

		private PersistenceFile _currentPersistFile;
		private PersistenceFileWatcher _pfsWatcher;

		public PersistenceService() :
			this(x => { return x;})
		{ }

		public PersistenceService(Func<string, string> getFullFilePath)
		{
			_getFullFilePath = getFullFilePath;
			_tokenSource = new CancellationTokenSource();

		}

		public async Task Start(string initialFile)
		{
			InitialFile = initialFile;
			await FileSwitch.ForEachAsync<string>(async fileName => await PersistFile(fileName), _tokenSource.Token);
		}

		public void Stop()
		{
			_tokenSource.Cancel();
			_pfsWatcher.StopWatching();
			_currentPersistFile.Dispose();
		}

		public IObservable<TransactionElements> TransactionElementsSource
		{
			get
			{
				return _transactionsSource.AsObservable<TransactionElements>();
			}
		}

		private string InitialFile { get; set; }

		private IObservable<string> FileSwitch
		{
			get
			{
				return Observable.Return(InitialFile).Concat(this._fileSwitched.AsObservable());
			}
		}

		private async Task PersistFile(string file)
		{
			if (_pfsWatcher != null)
				_pfsWatcher.StopWatching();

			if (_currentPersistFile != null)
				_currentPersistFile.Dispose();

			_currentPersistFile = new PersistenceFile(file);

			_pfsWatcher = new PersistenceFileWatcher(_currentPersistFile, new SystemFileWatcherWrapper(new FileInfo(file).FullName));

			var elementObservable = _currentPersistFile.ElementSource.Subscribe(element => ParseElement(element), () => {
				if (_currentPersistFile.IsFileSwitch)
				{
					_fileSwitched.OnNext(_getFullFilePath(_currentPersistFile.NextFileName));
				}
				else if (_currentPersistFile.IsStop)
				{
					_transactionsSource.OnCompleted();
					_fileSwitched.OnCompleted();
				}
				});

			await _currentPersistFile.Read();
			
		}

		private void ParseElement(XElement element)
		{
			const long INVALID_TRANS_ID = -1;
			long transaction_id;
			if (element.Name == "lzMessage")
			{
				if (!long.TryParse(element.Attribute("transaction_id").Value ?? INVALID_TRANS_ID.ToString(), out transaction_id))
					transaction_id = INVALID_TRANS_ID;

				TransactionElements transactionElements = new TransactionElements(transaction_id);
				foreach (var child in element.Descendants())
				{
					transactionElements.Elements.Add(new TransactionElement(transaction_id, child.Name.LocalName, child.Attribute("action").Value ?? string.Empty,
						child.Attributes().ToDictionary(attr => attr.Name.LocalName, attr => attr.Value)));
				}
				_transactionsSource.OnNext(transactionElements);
			}
			else
			{
				// Hmmm not sure we are interested in much else?
			}
		}

	}

}
