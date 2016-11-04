using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace FeedControl.Controllers
{
  [Route("api")]
  public class ApiController : Controller
  {
    private const string FEED_HOUR_KEY = "FEED_HOUR";
    private const string FEED_NOW_KEY = "FEED_NOW";
    private const string LAST_PING = "LAST_PING";

    private const string CAM_IMAGE_CACHE_KEY = "CAM_IMAGE_CACHE_KEY";
    private const string CAM_LAST_UPDATE_CACHE_KEY = "CAM_LAST_UPDATE_CACHE_KEY";
    private const string CAM_LAST_REQUEST_CACHE_KEY = "CAM_LAST_REQUEST_CACHE_KEY";

    //private readonly IOptions<FeedConfig> _config;
    private readonly IHostingEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public ApiController(IOptions<FeedConfig> config, 
      IHostingEnvironment environment, 
      IMemoryCache memoryCache)
    {
      //this._config = config;
      this._environment = environment;
      this._memoryCache = memoryCache;
    }

    [Route("oktofeed"), HttpGet]
    public bool CheckOkToFeed()
    {
      using (var db = new DataContext())
      {
        var hourStr = db.Settings.First(s => s.Key == FEED_HOUR_KEY).Value;
        var feedHour = Convert.ToInt32(hourStr);

        var now = DateTime.Now;
        var lastFeedTime =
          db.FeedLogs.OrderByDescending(l => l.EntryTime).FirstOrDefault(l => l.Type == FeedLogType.FeedDone);
        var isFeedNow = db.Settings.First(s => s.Key == FEED_NOW_KEY).Value == "1";

        if (isFeedNow || lastFeedTime != null && lastFeedTime.EntryTime.Day < now.Day && feedHour == now.Hour)
        {
          db.Settings.First(s => s.Key == FEED_NOW_KEY).Value = "0";
          db.SaveChanges();

          return true;
        }
        else
        {
          db.Settings.First(s => s.Key == LAST_PING).Value = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss");
          db.SaveChanges();
          return false;
        }
      }
    }

    [Route("getfeedlogs"), HttpGet]
    public IEnumerable<FeedLog> GetFeedLogs()
    {
      using (var db = new DataContext())
      {
        var logs = db.FeedLogs.Where(l => l.Type == FeedLogType.FeedDone)
          .OrderByDescending(l => l.EntryTime).ToList();

        foreach (var feedLog in logs)
        {
          feedLog.Pics = getImagePaths(feedLog.EntryTime);
        }

        return logs;
      }
    }

    [Route("getstatus"), HttpGet]
    public JsonResult GetStatus()
    {
      using (var db = new DataContext())
      {
        var hourStr = db.Settings.First(s => s.Key == FEED_HOUR_KEY).Value;
        var lastFeed =
          db.FeedLogs.OrderByDescending(l => l.EntryTime).FirstOrDefault(l => l.Type == FeedLogType.FeedDone);
        var lastFeedTime = lastFeed == null ? DateTime.MinValue : lastFeed.EntryTime;
        var lastPingTime = db.Settings.First(s => s.Key == LAST_PING).Value;

        return new JsonResult(
          new
          {
            FeedHour = hourStr,
            LastFeedTime = lastFeedTime,
            LastPingTime = lastPingTime,
            Pics = getImagePaths(lastFeedTime)
          });
      }
    }

    [Route("feednow"), HttpGet]
    public void FeedNow()
    {
      using (var db = new DataContext())
      {
        db.Settings.First(s => s.Key == FEED_NOW_KEY).Value = "1";
        db.SaveChanges();
      }
    }

    [Route("feeddone"), HttpGet]
    public void LogFeedDone()
    {
      using (var db = new DataContext())
      {
        db.FeedLogs.Add(new FeedLog() {EntryTime = DateTime.Now, Type = FeedLogType.FeedDone});
        db.SaveChanges();
      }
    }

    [Route("logerror"), HttpGet]
    public void LogError(string msg)
    {
      using (var db = new DataContext())
      {
        db.FeedLogs.Add(new FeedLog() { EntryTime = DateTime.Now, Type = FeedLogType.Error, Content = msg});
        db.SaveChanges();
      }
    }

    [Route("geterrors"), HttpGet]
    public IEnumerable<FeedLog> GetErrors()
    {
      using (var db = new DataContext())
      {
        var logs = db.FeedLogs.Where(l => l.Type == FeedLogType.Error)
          .OrderByDescending(l => l.EntryTime).ToList();

        return logs;
      }
    }

    [Route("clearerrorlogs"), HttpGet]
    public void ClearErrorLogs()
    {
      using (var db = new DataContext())
      {
        db.FeedLogs.RemoveRange(db.FeedLogs.Where(l => l.Type == FeedLogType.Error));
        db.SaveChanges();
      }
    }

    [Route("clearfeedlogs"), HttpGet]
    public void ClearFeedLogs()
    {
      using (var db = new DataContext())
      {
        db.FeedLogs.RemoveRange(db.FeedLogs.Where(l => l.Type == FeedLogType.FeedDone));
        db.SaveChanges();
      }
    }

    [Route("uploadimages"), HttpPost]
    public async Task UploadImages()
    {
      var uploads = Path.Combine(_environment.WebRootPath, "uploads");

      var files = Request.Form.Files;
      if (files.Any())
      {
        var dateStr = files.ElementAt(0).FileName.Split('_')[0];
        foreach (var file in files)
        {
          if (file.Length > 0)
          {
            if (!Directory.Exists(Path.Combine(uploads, dateStr)))
              Directory.CreateDirectory(Path.Combine(uploads, dateStr));

            using (var fileStream = new FileStream(Path.Combine(uploads, dateStr, file.FileName), FileMode.Create))
            {
              await file.CopyToAsync(fileStream);
            }
          }
        }
      }
    }

    private List<string> getImagePaths(DateTime date)
    {
      var dir = Path.Combine(_environment.WebRootPath, "uploads", date.ToString("yyyyMMdd"));
      if (Directory.Exists(dir))
      {
        return Directory.GetFiles(dir).Select(f => $"{date.ToString("yyyyMMdd")}/{Path.GetFileName(f)}").ToList();
      }
      else
      {
        return null;
      }
    }

    [Route("ping"), HttpGet]
    public string Ping()
    {
      return DateTime.Now.ToString();
    }

    [Route("dbping"), HttpGet]
    public string DbPing()
    {
      try
      {
        using (var db = new DataContext())
        {
          return db.Settings.First(s => s.Key == FEED_HOUR_KEY).Value;
        }
      }
      catch (Exception e)
      {
        return e.Message + e.InnerException.Message;
      }
    }

    [Route("streamup"), HttpPost]
    public async Task StreamUp()
    {
      var files = Request.Form.Files;
      if (files.Any())
      {
        var file = files.ElementAt(0);
        _memoryCache.Set(CAM_LAST_UPDATE_CACHE_KEY, DateTime.Now);

        using (var memoryStream = new MemoryStream())
        {
          await file.CopyToAsync(memoryStream);
          _memoryCache.Set(CAM_IMAGE_CACHE_KEY, memoryStream.ToArray());
        }
      }
    }

    [Route("streamdown"), HttpGet]
    public byte[] StreamDown()
    {
      _memoryCache.Set(CAM_LAST_REQUEST_CACHE_KEY, DateTime.Now);

      return (byte[])_memoryCache.Get(CAM_IMAGE_CACHE_KEY);
    }

    [Route("startstream"), HttpGet]
    public bool CheckIfStreamingUp()
    {
      DateTime lastRequest;
      if (_memoryCache.Get(CAM_LAST_REQUEST_CACHE_KEY) == null)
        lastRequest = DateTime.MinValue;
      else
        lastRequest = (DateTime) _memoryCache.Get(CAM_LAST_REQUEST_CACHE_KEY);

      return DateTime.Now.Subtract(lastRequest).Seconds < 30;
    }
  }
}
