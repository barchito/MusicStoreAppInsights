using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicStore.Models;
using MusicStore.ViewModels;
using Microsoft.ApplicationInsights;
using System.Collections.Generic;

namespace MusicStore.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly ILogger<ShoppingCartController> _logger;
        private readonly TelemetryClient _telemtry;

        public ShoppingCartController(MusicStoreContext dbContext, ILogger<ShoppingCartController> logger, TelemetryClient telemetry)
        {
            DbContext = dbContext;
            _logger = logger;
            _telemtry = telemetry;
        }

        public MusicStoreContext DbContext { get; }

        //
        // GET: /ShoppingCart/
        public async Task<IActionResult> Index()
        {
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            // Set up our ViewModel
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = await cart.GetCartItems(),
                CartTotal = await cart.GetTotal()
            };

            _telemtry.TrackEvent("Cart", new Dictionary<string, string> { { "total", viewModel.CartTotal.ToString() }, { "items", viewModel.CartItems.ToString() } });

            // Return the view
            return View(viewModel);
        }

        //
        // GET: /ShoppingCart/AddToCart/5

        public async Task<IActionResult> AddToCart(int id, CancellationToken requestAborted)
        {
            // Retrieve the album from the database
            var addedAlbum = await DbContext.Albums
                .SingleAsync(album => album.AlbumId == id);

            // Add it to the shopping cart
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            await cart.AddToCart(addedAlbum);

            await DbContext.SaveChangesAsync(requestAborted);
            _logger.LogInformation("Album {albumId} was added to the cart.", addedAlbum.AlbumId);
            _telemtry.TrackEvent("Add Cart Item", new Dictionary<string, string> { { "price", addedAlbum.Price.ToString() }, { "pricecategory", addedAlbum.PriceCategory.ToString() }, { "genre", addedAlbum.Genre?.Name } });


            // Go back to the main store page for more shopping
            return RedirectToAction("Index");
        }

        //
        // AJAX: /ShoppingCart/RemoveFromCart/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(
            int id,
            CancellationToken requestAborted)
        {
            // Retrieve the current user's shopping cart
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            // Get the name of the album to display confirmation
            var cartItem = await DbContext.CartItems
                .Where(item => item.CartItemId == id)
                .Include(c => c.Album)
                .SingleOrDefaultAsync();

            string message;
            int itemCount;
            if (cartItem != null)
            {
                // Remove from cart
                itemCount = cart.RemoveFromCart(id);

                await DbContext.SaveChangesAsync(requestAborted);

                string removed = (itemCount > 0) ? " 1 copy of " : string.Empty;
                message = removed + cartItem.Album.Title + " has been removed from your shopping cart.";
            }
            else
            {
                itemCount = 0;
                message = "Could not find this item, nothing has been removed from your shopping cart.";
            }

            // Display the confirmation message

            var results = new ShoppingCartRemoveViewModel
            {
                Message = message,
                CartTotal = await cart.GetTotal(),
                CartCount = await cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };

            _logger.LogInformation("Album {id} was removed from a cart.", id);
            _telemtry.TrackEvent("Remove Cart Item", new Dictionary<string, string> { { "price", cartItem.Album.Price.ToString() }, { "pricecategory", cartItem.Album.PriceCategory.ToString() }, { "genre", cartItem.Album.Genre?.Name } });


            return Json(results);
        }
    }
}