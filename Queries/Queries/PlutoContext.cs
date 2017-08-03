using System.Data.Entity;
using Queries.EntityConfigurations;

namespace Queries
{
    public class PlutoContext : DbContext
    {
        public PlutoContext()
            : base("name=PlutoContext")
        {
            // Serve para desativar o LazyLoading que vem ativo por padr�o. Com o lazy loading ativo, � necess�rio
            // usar o m�todo Include() para fazer um carregamento Eager expl�cito.
            //this.Configuration.LazyLoadingEnabled = true;
        }

        public virtual DbSet<Author> Authors { get; set; }
        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new CourseConfiguration());
        }
    }
}
