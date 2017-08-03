namespace CodeFirstExistingDB.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameTitleToNameInCoursesTable : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.Courses", "Title", "Name");
        }
        
        public override void Down()
        {
            RenameColumn("dbo.Courses", "Name", "Title");
        }
    }
}
