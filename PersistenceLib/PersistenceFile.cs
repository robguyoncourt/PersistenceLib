using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Persistence
{
	public class PersistenceFile: IDisposable
	{

		private static Regex s_fileSwitchRegex = new Regex(@"(Switch to file )(.+\.[a-zA-Z]{3})", RegexOptions.Compiled);
		private static Regex s_startLoggingRegex = new Regex(@"start logging", RegexOptions.Compiled);
		private static Regex s_stopLoggingRegex = new Regex(@"stop logging", RegexOptions.Compiled);
		private const int fileSwtichRegexFileNameGroup = 2;

		private readonly XmlReader _xmlReader;
		private bool _disposed = false;

		public string NextFileName { get; private set; }
		public bool IsFileSwitch { get; private set; }
		public bool IsStop { get; private set; }

		private readonly Subject<XElement> _elementSubject;
		/// <summary>
		/// Parameterless constructor for mock
		/// </summary>
		public PersistenceFile()
		{}

		public PersistenceFile(string path)
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				throw new ArgumentException("Filename " + path + " is invalid");

			_xmlReader = XmlReader.Create(new FileStream(path, FileMode.Open, FileAccess.Read), CreateXMLReaderSettings());

			_elementSubject = new Subject<XElement>();

		}

		private  XmlReaderSettings CreateXMLReaderSettings()
		{
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.Async = true;
			readerSettings.IgnoreComments = false;
			readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
			return readerSettings;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				if (_xmlReader != null)
				{
					_xmlReader.Close();
					_xmlReader.Dispose();
				}
			}

			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		public IObservable<XElement> ElementSource
		{
			get { return _elementSubject.AsObservable<XElement>(); }
		}

		public virtual async Task Read()
		{
			try
			{
				// Define the cancellation token.
				CancellationTokenSource source = new CancellationTokenSource();
				CancellationToken token = source.Token;

				while (await _xmlReader?.ReadAsync())
				{

					switch (_xmlReader.NodeType)
					{
						case XmlNodeType.Element:
							var element = await XNode.ReadFromAsync(_xmlReader, token) as XElement;

							if (element != null)
							{
								_elementSubject.OnNext(element);
							}
							break;

						case XmlNodeType.Comment:
							var comment = await XNode.ReadFromAsync(_xmlReader, token) as XComment;
							Match fileSwitchMatch = s_fileSwitchRegex.Match(comment.Value);
							IsFileSwitch = fileSwitchMatch.Success;
							if (IsFileSwitch)
								NextFileName = fileSwitchMatch.Groups[fileSwtichRegexFileNameGroup].Value;

							Match isStopMatch = s_stopLoggingRegex.Match(comment.Value);
							IsStop = isStopMatch.Success;
							if (IsStop || IsFileSwitch)
								_elementSubject.OnCompleted();

							break;
					}

				}
			}
			catch (Exception ex)
			{
				_elementSubject.OnError(ex);
			}
		}
	}
}

