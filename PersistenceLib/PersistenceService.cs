using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Xml.Linq;
using System.Linq;
using System.Reactive.Concurrency;
using System.Collections.Generic;

namespace Persistence
{
	/// <summary>
	/// Takes an intitial persistence file when started log updates are turned into an observable
	/// sequence
	/// </summary>
	public class PersistenceService
	{
		private const long INVALID_TRANS_ID = -1;

		private readonly Func<string, string> _getFullFilePath;
		private readonly CancellationTokenSource _tokenSource;
		private readonly EventLoopScheduler _scheduler;

		private readonly Dictionary<string, ActionType> _actionToActionTypeMap;
		private readonly Dictionary<string, (string key, string name)> _elementNameMap;

		private Subject<string> _fileSwitched = new Subject<string>();
		private Subject<TransactionElements> _transactionsSource = new Subject<TransactionElements>();

		private PersistenceFile _currentPersistFile;
		private IPersistDestination _persistDesitination;

		private long _initalTransactionId = INVALID_TRANS_ID;
		private string _initialFile;

		public PersistenceService() :
			this(x => { return x;}, null)
		{ }

		public PersistenceService(Func<string, string> getFullFilePath, IPersistDestination persistDestination)
		{
			_getFullFilePath = getFullFilePath;
			_tokenSource = new CancellationTokenSource();
			_scheduler = new EventLoopScheduler();

			_actionToActionTypeMap = new Dictionary<string, ActionType>
			{
				{"", ActionType.Unknown },
				{"Amended", ActionType.Update },
				{"Added", ActionType.Insert},
				{"CancelledStatusUpdated", ActionType.Update },
				{"RolledOver", ActionType.Update },
				{"Sent", ActionType.Update },
				{"Demoted", ActionType.Update },
				{"PostConfirmed", ActionType.Update }

			};
			
			_elementNameMap = new Dictionary<string, (string key, string name)>
			{
				{"lzControl", ("control_id","control_messages")},
				{"lzExecutionMerge", ("merge_id","execution_merges")},
				{"lzExecution", ("execution_id","executions")},
				{"lzPairOff", ("pairoff_id","pairoffs")},
				{"lzAudit", ("audit_event_id","audit_events") },
				{"lzRelease", ("release_id","releases")},
				{"lzAllocation", ("allocation_id", "allocations")},
				{"lzStrategy", ("strategy_id", "strategies")},
				{"lzOrder", ("order_id", "orders")},
				{"lzListOrder", ("list_id", "list_orders")},
				{"lzMarketList", ("market_list_id", "market_lists")},
				{"lzQuote", ("quote_id", "quotes")},
				{"lzQuoteList", ("quote_list_id", "quote_lists") },
				{"lzParameter", ("parameter", "minerva_params")},
				{"lzContingencyGroup", ("contingency_group_id", "contingency_groups")},
				{"lzContingencyLink", ("contingency_link_Id", "contingency_links")},
				{"lzCharge", ("charge_id", "charges")},
				{"lzCollateral", ("collateral_id", "collaterals")},
			};

			_persistDesitination = persistDestination;
		}

		public async Task Start(string initialFile)
		{
			
			_initialFile = initialFile;

			_transactionsSource.ObserveOn(_scheduler).Subscribe(te => { _persistDesitination?.Persist(te); }, () => { }, _tokenSource.Token);

			await FileSwitch.ForEachAsync<string>(async fileName => await PersistFile(fileName), _tokenSource.Token);
		}

		public async Task StartFromTransaction(string initialFile, long transactionId)
		{
			_initalTransactionId = transactionId;

			await Start(initialFile);
		}

		public void Stop()
		{
			_tokenSource.Cancel();
			_currentPersistFile.Dispose();
			_scheduler.Dispose();
		}

		public IObservable<TransactionElements> TransactionElementsSource
		{
			get
			{
				return _transactionsSource.AsObservable<TransactionElements>();
			}
		}

		private IObservable<string> FileSwitch
		{
			get
			{
				return Observable.Return(_initialFile).Concat(this._fileSwitched.AsObservable());
			}
		}

		private async Task PersistFile(string file)
		{
	
			if (_currentPersistFile != null)
				_currentPersistFile.Dispose();

			_currentPersistFile = new PersistenceFile(file);

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
			long transaction_id;
			if (element.Name == "lzMessage")
			{
				if (!long.TryParse(element.Attribute("transaction_id").Value ?? INVALID_TRANS_ID.ToString(), out transaction_id))
					transaction_id = INVALID_TRANS_ID;

				if (transaction_id > INVALID_TRANS_ID && transaction_id >= _initalTransactionId)
				{
					TransactionElements transactionElements = new TransactionElements(transaction_id);
					foreach (var child in element.Descendants())
					{

						string action = child.Attribute("action").Value ?? string.Empty;
						if (!_actionToActionTypeMap.ContainsKey(action))
							_actionToActionTypeMap.Add(action, ActionType.Unknown);

						string elementName = child.Name.LocalName;
						if (!_elementNameMap.ContainsKey(elementName))
							_elementNameMap.Add(elementName, (string.Empty, string.Empty));

						transactionElements.Elements.Add(
							new TransactionElement(transaction_id,_elementNameMap[elementName].name,
							_elementNameMap[elementName].key,
							_actionToActionTypeMap[action],
							child.Attributes().ToDictionary(attr => attr.Name.LocalName.ToLower(), attr => attr.Value)));
					}
					_transactionsSource.OnNext(transactionElements);
				}
			}
			else
			{
				// Hmmm not sure we are interested in much else?
			}
		}

	}

}
