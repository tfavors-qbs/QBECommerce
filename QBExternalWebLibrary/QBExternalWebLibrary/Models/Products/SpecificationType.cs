namespace QBExternalWebLibrary.Models.Products
{
    public abstract class SpecificationType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
    }
    public class Shape : SpecificationType { }
    public class Material : SpecificationType { }
    public class Coating : SpecificationType { }
    public class Thread : SpecificationType { }
    public class Spec : SpecificationType { }
}
