using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MusicStore.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using System.Collections.Generic;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public StoreController(MusicStoreContext dbContext, IOptions<AppSettings> options, TelemetryClient telemetry)
        {
            DbContext = dbContext;
            _appSettings = options.Value;
            _telemetry = telemetry;
        }

        public MusicStoreContext DbContext { get; }

        //
        // GET: /Store/
        public async Task<IActionResult> Index()
        {
            var genres = await DbContext.Genres.ToListAsync();

            return View(genres);
        }

        //
        // GET: /Store/Browse?genre=Disco
        public async Task<IActionResult> Browse(string genre)
        {
            // Retrieve Genre genre and its Associated associated Albums albums from database
            var genreModel = await DbContext.Genres
                .Include(g => g.Albums)
                .Where(g => g.Name == genre)
                .FirstOrDefaultAsync();

            if (genreModel == null)
            {
                return NotFound();
            }


            _telemetry.TrackEvent("Browser", new Dictionary<string, string> { { "genre", genre } });
            return View(genreModel);
        }

        public async Task<IActionResult> Details(
            [FromServices] IMemoryCache cache,
            int id)
        {
            if (id == 6) throw new ArgumentException("I dont like this album!");
            var cacheKey = string.Format("album_{0}", id);
            Album album;
            if (!cache.TryGetValue(cacheKey, out album))
            {
                album = await DbContext.Albums
                                .Where(a => a.AlbumId == id)
                                .Include(a => a.Artist)
                                .Include(a => a.Genre)
                                .FirstOrDefaultAsync();

                if (album != null)
                {
                    if (_appSettings.CacheDbResults)
                    {
                        //Remove it from cache if not retrieved in last 10 minutes
                        cache.Set(
                            cacheKey,
                            album,
                            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                    }
                }
            }

            if (album == null)
            {
                return NotFound();
            }
            _telemetry.TrackEvent("product", new Dictionary<string, string> { { "album", album.Title }, { "genre", album.Genre?.Name } });
            return View(album);
        }
    }
}