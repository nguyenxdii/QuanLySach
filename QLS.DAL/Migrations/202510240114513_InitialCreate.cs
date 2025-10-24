namespace QLS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LoaiSach",
                c => new
                    {
                        MaLoai = c.Int(nullable: false, identity: true),
                        TenLoai = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.MaLoai);
            
            CreateTable(
                "dbo.Sach",
                c => new
                    {
                        MaSach = c.String(nullable: false, maxLength: 6, fixedLength: true, unicode: false),
                        TenSach = c.String(nullable: false, maxLength: 200),
                        NamXB = c.Int(nullable: false),
                        MaLoai = c.Int(nullable: false),
                        HinhAnh = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.MaSach)
                .ForeignKey("dbo.LoaiSach", t => t.MaLoai)
                .Index(t => t.MaLoai);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Sach", "MaLoai", "dbo.LoaiSach");
            DropIndex("dbo.Sach", new[] { "MaLoai" });
            DropTable("dbo.Sach");
            DropTable("dbo.LoaiSach");
        }
    }
}
