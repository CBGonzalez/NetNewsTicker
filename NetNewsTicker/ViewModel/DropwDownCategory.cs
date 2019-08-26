
namespace NetNewsTicker.ViewModels
{   
    public class DropDownCategory : System.IEquatable<DropDownCategory>
    {
        public string Name { get; }
        public int Id { get; }

        public DropDownCategory(int id, string name)
        {
            Name = name;
            Id = id;
        }

        public bool Equals(DropDownCategory other)
        {
            if (other == null)
            {
                return false;
            }
            return other.Id == Id;
        }

        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }

            return obj is DropDownCategory && Equals((DropDownCategory)obj);
        }

        
        public override int GetHashCode()
        {
            return Id;
        }        
    }
    
}
