namespace CodeFirstExistingDB.Migrations
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<CodeFirstExistingDB.PlutoModel>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(CodeFirstExistingDB.PlutoModel context)
        {
            context.Authors.AddOrUpdate(a => a.Name,
                new Author
                {
                    Name = "Author 2",
                    Courses = new Collection<Course>()
                    {
                        new Course() { Name = "Course 1", Description = "Description 2"}
                    }
                });
        }
    }
}
