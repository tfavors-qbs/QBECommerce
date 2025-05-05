
using System.ComponentModel;
using QBExternalWebLibrary.Models.Products;
using static QBExternalWebLibrary.Models.Products.ProductID;

namespace QBExternalWebLibrary.Models.Products
{
    public class ProductID
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public int ShapeId { get; set; }
        public Shape Shape { get; set; }
        public int MaterialId { get; set; }
        public Material Material { get; set; }
        public int CoatingId { get; set; }
        public Coating Coating { get; set; }
        public int ThreadId { get; set; }
        public Thread Thread { get; set; }
        public int SpecId { get; set; }
        public Spec Spec { get; set; }
        [DisplayName("Legacy ID")]
        public int LegacyId { get; set; }
        [DisplayName("Legacy Name")]
        public string LegacyName { get; set; }
        public string? Description { get; set; }
        //public ProductIDEditViewModel ToEVM() {
        //    return new ProductIDEditViewModel {
        //        Id = Id,
        //        GroupId = GroupId,
        //        ShapeId = ShapeId,
        //        MaterialId = MaterialId,
        //        CoatingId = CoatingId,
        //        ThreadId = ThreadId,
        //        SpecId = SpecId,
        //        LegacyId = LegacyId,
        //        LegacyName = LegacyName,
        //        Description = Description,
        //    };
        //}
    }

    public class ProductIDEditViewModel
    {
        public int Id { get; set; }
        [DisplayName("Group")]
        public int GroupId { get; set; }
        [DisplayName("Shape")]
        public int ShapeId { get; set; }
        [DisplayName("Material")]
        public int MaterialId { get; set; }
        [DisplayName("Coating")]
        public int CoatingId { get; set; }
        [DisplayName("Thread")]
        public int ThreadId { get; set; }
        [DisplayName("Spec")]
        public int SpecId { get; set; }
        [DisplayName("Legacy ID")]
        public int LegacyId { get; set; }
        [DisplayName("Legacy Name")]
        public string LegacyName { get; set; }
        public string? Description { get; set; }
        //public ProductID ToModel(DataContext context) {
        //    ProductID productID = context.ProductIDs.Find(Id);
        //    if (productID == null) { productID = new ProductID(); }
        //    productID.Id = Id;
        //    productID.GroupId = GroupId;
        //    productID.ShapeId = ShapeId;
        //    productID.MaterialId = MaterialId;
        //    productID.CoatingId = CoatingId;
        //    productID.ThreadId = ThreadId;
        //    productID.SpecId = SpecId;
        //    productID.LegacyId = LegacyId;
        //    productID.LegacyName = LegacyName;
        //    productID.Description = Description;
        //    productID.Group = context.Groups.Find(GroupId);
        //    productID.Shape = context.Shapes.Find(ShapeId);
        //    productID.Material = context.Materials.Find(MaterialId);
        //    productID.Coating = context.Coatings.Find(CoatingId);
        //    productID.Thread = context.Threads.Find(ThreadId);
        //    productID.Spec = context.Specs.Find(SpecId);
        //    return productID;
        //}
    }
}
