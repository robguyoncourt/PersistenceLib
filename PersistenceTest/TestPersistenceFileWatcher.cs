using Moq;
//using NUnit.Framework;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Xml;

namespace Persistence.UnitTests
{
	[TestClass]
	public class TestPersistenceFileWatcher
	{
	
		[TestMethod]
		public void SystemFileWatcherContentChangedRaisesRead()
		{

			//arrange
			Mock<PersistenceFile> mockPersistenceFile = new Mock<PersistenceFile>();
			Mock<SystemFileWatcherWrapper> mockSystemFileWatcher = new Mock<SystemFileWatcherWrapper>();
			PersistenceFileWatcher pfw = new PersistenceFileWatcher(mockPersistenceFile.Object, mockSystemFileWatcher.Object);

			// act
			mockSystemFileWatcher.Raise(mock => mock.ContentChanged += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, string.Empty, string.Empty)); // this does not fire

			// assert and verify
			mockPersistenceFile.Verify(mock => mock.Read());
		}


	}
}
