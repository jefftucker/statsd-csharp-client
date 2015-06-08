using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClientTests
{
  [TestClass]
  public class StatsdExtensionsTests
  {
    private FakeOutputChannel _channel;
    private Statsd _statsd;
    private TestData _testData;
    TaskCompletionSource<int> _tcs;

    [TestInitialize]
    public void Initialise()
    {
      _channel = new FakeOutputChannel();
      _statsd = new Statsd("localhost", 12000, outputChannel : _channel);
      _testData = new TestData();
    }

    [TestMethod]
    public void count_SendToStatsd_Success()
    {        
      _statsd.count().foo.bar += 1;
      Assert.AreEqual<string>("foo.bar:1|c", _channel.LineSent);
    }

    [TestMethod]
    public void gauge_SendToStatsd_Success()
    {
      _statsd.gauge().foo.bar += 1;
      Assert.AreEqual<string>("foo.bar:1|g", _channel.LineSent);
    }

    [TestMethod]
    public void timing_SendToStatsd_Success()
    {      
      _statsd.timing().foo.bar += 1;
      Assert.AreEqual<string>("foo.bar:1|ms", _channel.LineSent);
    }

    [TestMethod]
    public void count_AddNamePartAsString_Success()
    {
      _statsd.timing().foo._("bar")._ += 1;
      Assert.AreEqual<string>("foo.bar:1|ms", _channel.LineSent);
    }
  }
  
}
