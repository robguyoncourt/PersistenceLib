using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Persistence
{
	/// <summary>
	/// Wrapper on FileSystemWatcher
	/// </summary>
	public class SystemFileWatcherWrapper
	{
		public virtual event FileSystemEventHandler ContentChanged;
			
		private FileSystemWatcher _fileSystemWatcher;


		public SystemFileWatcherWrapper(string fileToWatch) :
			this()
		{
			StartWatching(fileToWatch);
		}

		public SystemFileWatcherWrapper()
		{
			_fileSystemWatcher = new FileSystemWatcher();
		}

		private void m_fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Changed)
			{
				ContentChanged?.Invoke(this, e);
			}
		}

		public virtual void StartWatching(string fileToWatch)
		{
			_fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
			FileInfo fi = new FileInfo(fileToWatch);
			_fileSystemWatcher.Path = fi.DirectoryName;
			_fileSystemWatcher.Filter = fi.Name;

			_fileSystemWatcher.Changed += m_fileSystemWatcher_Changed;
			_fileSystemWatcher.EnableRaisingEvents = true;
		}
	
		public virtual void StopWatching()
		{ 
			_fileSystemWatcher.EnableRaisingEvents = false;
			_fileSystemWatcher.Changed -= m_fileSystemWatcher_Changed;

		}

	}
}
