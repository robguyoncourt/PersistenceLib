using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;


namespace Persistence.UnitTests
{
	[TestClass]
	public class TestPersistenceFile
	{
		[TestMethod]
		public async Task SmallTestFileParsesCorrectly()
		{
			TestHelper th = new TestHelper();
			int count = 0;
			bool done = false;
			PersistenceFile pf = new PersistenceFile(th.GetResourceTextFile(th.SMALLTESTFILE));
			pf.ElementSource.Subscribe(x => count++, () => done = true);
			await pf.Read();
			Assert.AreEqual(1, count);
			Assert.IsTrue(done);
			Assert.IsTrue(pf.IsStop);
		}

		[TestMethod]
		public async Task MediumTestFileParsesCorrectly()
		{
			TestHelper th = new TestHelper();
			int count = 0;
			bool done = false;
			PersistenceFile pf = new PersistenceFile(th.GetResourceTextFile(th.MEDIUMTESTFILE));
			pf.ElementSource.Subscribe(x => count++, () => done = true);
			await pf.Read();
			Assert.AreEqual(4, count);
			Assert.IsTrue(done);
			Assert.IsTrue(pf.IsStop);

		}

		[TestMethod]
		public async Task MediumTestSwitchFileParsesCorrectly()
		{
			TestHelper th = new TestHelper();
			int count = 0;
			bool done = false;
			PersistenceFile pf = new PersistenceFile(th.GetResourceTextFile(th.MEDIUMTESTSWITCH1FILE));
			pf.ElementSource.Subscribe(x => count++, () => done = true);
			await pf.Read();
			Assert.AreEqual(4, count);
			Assert.IsTrue(done);
			Assert.IsTrue(pf.IsFileSwitch);
			Assert.AreEqual(th.MEDIUMTESTSWITCH2FILE, pf.NextFileName);
		}
	}
}
