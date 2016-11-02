using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace FeedControl.Controllers
{
  [Route("api")]
  public class ApiController : Controller
  {
    private const string FEED_HOUR_KEY = "FEED_HOUR";
    private const string FEED_NOW_KEY = "FEED_NOW";

    private readonly IOptions<FeedConfig> _config;
    private readonly IHostingEnvironment _environment;

    public ApiController(IOptions<FeedConfig> config, IHostingEnvironment environment)
    {
      this._config = config;
      this._environment = environment;

      using (var db = new DataContext())
      {
        db.Database.Migrate();

        if (!db.Settings.Any(s => s.Key == FEED_HOUR_KEY))
        {
          db.Settings.Add(new Setting()
          {
            Key = FEED_HOUR_KEY,
            Value = this._config.Value.FeedHour.ToString()
          });
        }

        if (!db.Settings.Any(s => s.Key == FEED_NOW_KEY))
        {
          db.Settings.Add(new Setting()
          {
            Key = FEED_NOW_KEY,
            Value = "0"
          });
        }

        db.SaveChanges();
      }
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
          db.FeedLogs.Add(new FeedLog() {EntryTime = DateTime.Now, Type = FeedLogType.PingLog});
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
        var lastFeedTime =
          db.FeedLogs.OrderByDescending(l => l.EntryTime).FirstOrDefault(l => l.Type == FeedLogType.FeedDone).EntryTime;
        var lastPingTime =
          db.FeedLogs.OrderByDescending(l => l.EntryTime).FirstOrDefault(l => l.Type == FeedLogType.PingLog).EntryTime;

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

    [Route("uploadimages"), HttpPost]
    public async Task UploadImages(ICollection<IFormFile> files)
    {
      var uploads = Path.Combine(_environment.WebRootPath, "uploads");

      if (files.Any())
      {
        var dateStr = files.ElementAt(0).FileName.Split('_')[0];
        foreach (var file in files)
        {
          if (file.Length > 0)
          {
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
  }
}
