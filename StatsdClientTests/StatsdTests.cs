using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StatsdClient;
using Moq;
using System.Threading.Tasks;

namespace StatsdClientTests
{
    // Moq does a poor job of mocking the async calls
    // so this works much more reliably and accomplishes 
    // the same thing.
    public class FakeOutputChannel : IOutputChannel
    {        
        public string LineSent { get; private set; }
        public Task SendAsync(string line)
        {
            LineSent = line;
            return Task<int>.FromResult<int>(1);
        }
    }

  [TestClass]
  public class StatsdTests
  {
    private Mock<IOutputChannel> _outputChannel;
    private Statsd _statsd;
    private TestData _testData;
    FakeOutputChannel _channel;

    public StatsdTests()
    {
      _testData = new TestData();
    }

    [TestInitialize]
    public void Initialise()
    {
      _outputChannel = new Mock<IOutputChannel>();
      _channel = new FakeOutputChannel();
      _statsd = new Statsd("localhost", 12000, outputChannel : _channel);
    }

    // The async methods will throw exceptions wrapped in an AggregateException
    // so this function will unwrap the AggregateException and re-throw the 
    // correct inner exception.  If the exception type is different then it
    // will just re-throw it.
    private void UnwrapAggregateException(Action fun)
    {
        try
        {
            fun();
        }
        catch (AggregateException ex)
        {
            Exception exceptionToThrow = ex.InnerException;
            throw exceptionToThrow;
        }
        catch (Exception)
        {
            throw;
        }
    }

    #region Parameter Checks
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void LogCount_NameIsNull_ExpectArgumentNullException()
    {
        UnwrapAggregateException(() => { _statsd.LogCount(null); });        
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void LogCount_ValueIsLessThanZero_ExpectArgumentOutOfRangeException()
    {
        UnwrapAggregateException(() => { _statsd.LogCount("foo", -1); });      
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void LogGauge_NameIsNull_ExpectArgumentNullException()
    {
        UnwrapAggregateException(() => { _statsd.LogGauge(null, _testData.NextInteger); });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void LogGauge_ValueIsLessThanZero_ExpectArgumentOutOfRangeException()
    {
        UnwrapAggregateException(() => { _statsd.LogGauge("foo", -1); });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void LogTiming_NameIsNull_ExpectArgumentNullException()
    {
      UnwrapAggregateException(() => { _statsd.LogTiming(null, _testData.NextInteger); });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void LogTiming_ValueIsLessThanZero_ExpectArgumentOutOfRangeException()
    {
        UnwrapAggregateException(() => { _statsd.LogTiming("foo", -1); });
    }
    #endregion

    [TestMethod]
    public void LogCount_ValidInput_Success()
    {
        
      var stat = _testData.NextStatName;
      var count = _testData.NextInteger;    

      _statsd.LogCount(stat, count);
      Assert.AreEqual<string>(stat + ":" + count.ToString() + "|c", _channel.LineSent);
    }

    [TestMethod]
    public void LogTiming_ValidInput_Success()
    {
      var stat = _testData.NextStatName;
      var count = _testData.NextInteger;
      _statsd.LogTiming(stat, count);
      Assert.AreEqual<string>(_channel.LineSent, stat + ":" + count.ToString() + "|ms");
    }

    [TestMethod]
    public void LogGauge_ValidInput_Success()
    {
      var stat = _testData.NextStatName;
      var count = _testData.NextInteger;
      _statsd.LogGauge(stat, count);
      Assert.AreEqual<string>(_channel.LineSent, stat + ":" + count.ToString() + "|g");
    }

    [TestMethod]
    public void Constructor_PrefixEndsInPeriod_RemovePeriod()
    {
      var statsd = new Statsd("localhost", 12000, "foo.", outputChannel : _channel);
      var stat = _testData.NextStatName;
      var count = _testData.NextInteger;
      statsd.LogCount(stat, count);
      Assert.AreEqual<string>(_channel.LineSent, "foo." + stat + ":" + count.ToString() + "|c");
    }

    [TestMethod]
    public void LogCount_NullPrefix_DoesNotStartNameWithPeriod()
    {
      var statsd = new Statsd("localhost", 12000, prefix : null, outputChannel : _channel);      
      statsd.LogCount("some.stat");
      Assert.AreEqual<string>("some.stat:1|c", _channel.LineSent);     
    }

    [TestMethod]
    public void LogCount_EmptyStringPrefix_DoesNotStartNameWithPeriod()
    {
      var statsd = new Statsd("localhost", 12000, prefix : "", outputChannel : _outputChannel.Object);
      var inputStat = "some.stat:1|c";
      _outputChannel.Setup(p => p.SendAsync(It.Is<string>(q => q == inputStat)))
          .Returns(Task.FromResult<int>(1))
          .Verifiable();
        
      statsd.LogCount("some.stat");
      _outputChannel.VerifyAll();
    }

    [TestMethod]
    public void LogRaw_WithoutEpoch_Valid()
    {
      var statsd = new Statsd("localhost", 12000, prefix : "", outputChannel : _channel);
      statsd.LogRaw("my.raw.stat", 12934);
      Assert.AreEqual<string>("my.raw.stat:12934|r", _channel.LineSent);
    }

    [TestMethod]
    public void LogRaw_WithEpoch_Valid()
    {
      var statsd = new Statsd("localhost", 12000, prefix : "", outputChannel : _channel);
      var almostAnEpoch = DateTime.Now.Ticks;
      var inputStat = "my.raw.stat:12934|r|" + almostAnEpoch;      
      statsd.LogRaw("my.raw.stat", 12934, almostAnEpoch);
      Assert.AreEqual<string>(inputStat, _channel.LineSent);
    }

    [TestMethod]
    public void CreateClient_WithInvalidHostName_DoesNotError()
    {
      var statsd = new Statsd("nowhere.here.or.anywhere", 12000);
      statsd.LogCount("test.stat");
    }

    [TestMethod]
    public void CreateClient_WithIPAddress_DoesNotError()
    {
      var statsd = new Statsd("127.0.0.1", 12000);
      statsd.LogCount("test.stat");
    }

    [TestMethod]
    public void CreateClient_WithInvalidCharactersInHostName_DoesNotError()
    {
      var statsd = new Statsd("@%)(F(FSDLKDEQ423t0-vbdfb", 12000);
      statsd.LogCount("test.foo");
    }

    [TestMethod]
    public void CreateClient_WIthPrefixAndPostFix_NamesStatsCorrectly()
    {
        var statsd = new Statsd("127.0.0.1", 12000, prefix: "some.datacenter", postfix: "host", outputChannel: _channel);
        statsd.LogCount("some.stat");
        Assert.AreEqual<string>("some.datacenter.some.stat.host:1|c", _channel.LineSent);
    }
  }
}
