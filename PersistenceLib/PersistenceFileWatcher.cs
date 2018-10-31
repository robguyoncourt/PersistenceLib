
using System.IO;
using System.Threading.Tasks;

namespace Persistence
{
	/// <summary>
	/// Uses file system watcher object to check for updates to the persistence file
	/// Raises LineAdded event when a complete line has been written
	/// </summary>
	public class PersistenceFileWatcher
	{
		private readonly SystemFileWatcherWrapper _fsw = null;
		private readonly PersistenceFile _fileToWatch;

		public PersistenceFileWatcher(PersistenceFile fileToWatch, SystemFileWatcherWrapper fsw)
		{
			_fileToWatch = fileToWatch;

			_fsw = fsw;
			_fsw.ContentChanged += m_fsw_Changed;

		}

		public void StopWatching()
		{
			_fsw.StopWatching();
			_fsw.ContentChanged -= m_fsw_Changed;
		}

		private void m_fsw_Changed(object sender, FileSystemEventArgs e)
		{
			Task t = _fileToWatch.Read();
		}



	}



}
