using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MusicStore.Models
{
    public class Album
    {
        [ScaffoldColumn(false)]
        public int AlbumId { get; set; }

        public int GenreId { get; set; }

        public int ArtistId { get; set; }

        [Required]
        [StringLength(160, MinimumLength = 2)]
        public string Title { get; set; }

        [Required]
        [Range(0.01, 100.00)]

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public PriceCategory PriceCategory
        {
            get
            {
                if (Price >= 9) return PriceCategory.Expensive;
                if (Price > 5 && Price < 9) return PriceCategory.Normal;
                return PriceCategory.Cheap;

            }
        }

        [Display(Name = "Album Art URL")]
        [StringLength(1024)]
        public string AlbumArtUrl { get; set; }

        public virtual Genre Genre { get; set; }
        public virtual Artist Artist { get; set; }
        public virtual List<OrderDetail> OrderDetails { get; set; }

        [ScaffoldColumn(false)]
        [BindNever]
        [Required]
        public DateTime Created { get; set; }

        /// <summary>
        /// TODO: Temporary hack to populate the orderdetails until EF does this automatically. 
        /// </summary>
        public Album()
        {
            OrderDetails = new List<OrderDetail>();
            Created = DateTime.UtcNow;
        }
    }

    public enum PriceCategory {
        Cheap,
        Normal,
        Expensive
    }
}