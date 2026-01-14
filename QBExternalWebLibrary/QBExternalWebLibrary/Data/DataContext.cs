using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Models;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Ariba;

namespace QBExternalWebLibrary.Data
{
    public class DataContext : IdentityDbContext<ApplicationUser> {

        public DbSet<Client> Clients { get; set; }
        public DbSet<ContractItem> ContractItems { get; set; }
        public DbSet<SKU> SKUs { get; set; }
        public DbSet<ProductID> ProductIDs { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Shape> Shapes { get; set; }
        public DbSet<Length> Lengths { get; set; }
        public DbSet<Diameter> Diameters { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Coating> Coatings { get; set; }
        public DbSet<Thread> Threads { get; set; }
        public DbSet<Spec> Specs { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
		public DbSet<PunchOutSession> PunchOutSessions { get; set; }
		public DbSet<ErrorLog> ErrorLogs { get; set; }
		public DbSet<QuickOrder> QuickOrders { get; set; }
		public DbSet<QuickOrderItem> QuickOrderItems { get; set; }
		public DbSet<QuickOrderTag> QuickOrderTags { get; set; }
		public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            // Enforce one shopping cart per user with unique constraint
            modelBuilder.Entity<ShoppingCart>()
                .HasIndex(sc => sc.ApplicationUserId)
                .IsUnique()
                .HasDatabaseName("IX_ShoppingCarts_ApplicationUserId_Unique");
        }
    }

}
