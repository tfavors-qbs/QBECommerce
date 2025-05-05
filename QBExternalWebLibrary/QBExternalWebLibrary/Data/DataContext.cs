using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Models;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QBExternalWebLibrary.Models.Catalog;

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
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            //Add Specs
            //modelBuilder.Entity<Spec>().HasData(
            //    new Spec { Id = 1, Name = "~A194", DisplayName = "~A194" });

            ////Add Threads
            //modelBuilder.Entity<Thread>().HasData(
            //    new Thread { Id = 1, Name = "UNC", DisplayName = "UNC" });

            ////Add Coatings
            //modelBuilder.Entity<Coating>().HasData(
            //    new Coating { Id = 1, Name = "PLAIN", DisplayName = "Plain" });

            ////Add Materials
            //modelBuilder.Entity<Material>().HasData(
            //    new Material { Id = 1, Name = "2H", DisplayName = "2-H" });

            ////Add Shapes
            //modelBuilder.Entity<Shape>().HasData(
            //    new Shape { Id = 1, Name = "HVHX", DisplayName = "HeavyHx" });

            ////Add Classes
            //modelBuilder.Entity<Class>().HasData(
            //    new Class { Id = 1, LegacyId = "N", Name = "Nuts", DisplayName = "Nuts" });

            ////Add Groups
            //modelBuilder.Entity<Group>().HasData(
            //    new Group { Id = 1, ClassId = 1, LegacyId = "Y", Name = "HeavyHx", DisplayName = "HeavyHx" });

            ////Add ProductId
            //modelBuilder.Entity<ProductID>().HasData(
            //    new ProductID { Id = 1, GroupId = 1, ShapeId = 1, MaterialId = 1, CoatingId = 1, ThreadId = 1, SpecId = 1, LegacyId = 5, LegacyName = "NY005" });

            //modelBuilder.Entity<Diameter>().HasData(
            //    new Diameter { Id = 1, Name = "0.7500", DisplayName = "3/4\"", Value = .75 });

            //modelBuilder.Entity<SKU>().HasData(
            //    new SKU { Id = 1, DiameterId = 1, ProductIDId = 1, Name = "5VK" });

            //modelBuilder.Entity<Length>().HasData(
            //    new Length { Id = 1, Name = "1.00", DisplayName = "1.00", Value = 1.00 });

            //modelBuilder.Entity<Client>().HasData(
            //    new Client { Id = 1, Name = "TestClient", LegacyId = "6202"});

            //modelBuilder.Entity<ContractItem>().HasData(
            //    new ContractItem { Id = 1, CustomerStkNo = "TestContractItem", Description = "TestContractItem", Price = 25.00M, ClientId = 1, NonStock = false, SKUId = 1 });
        }
    }

}
