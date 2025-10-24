namespace QLS.DAL.Migrations
{
    using QLS.DAL.Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<QLS.DAL.QLSDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(QLS.DAL.QLSDbContext db)
        {
            // 1) Loại sách (>=4)
            var loais = new[]
            {
                new LoaiSach { TenLoai = "Khoa học" },
                new LoaiSach { TenLoai = "Văn học" },
                new LoaiSach { TenLoai = "CNTT" },
                new LoaiSach { TenLoai = "Kinh tế" },
                new LoaiSach { TenLoai = "Kỹ năng sống" }
            };
            foreach (var l in loais)
                db.LoaiSaches.AddOrUpdate(x => x.TenLoai, l);
            db.SaveChanges();

            // Lấy id cho tiện mapping
            int khoaHoc = db.LoaiSaches.Single(x => x.TenLoai == "Khoa học").MaLoai;
            int vanHoc = db.LoaiSaches.Single(x => x.TenLoai == "Văn học").MaLoai;
            int cntt = db.LoaiSaches.Single(x => x.TenLoai == "CNTT").MaLoai;
            int kinhTe = db.LoaiSaches.Single(x => x.TenLoai == "Kinh tế").MaLoai;
            int knSong = db.LoaiSaches.Single(x => x.TenLoai == "Kỹ năng sống").MaLoai;

            // 2) Sách (>=10) — MaSach phải đúng 6 ký tự
            var books = new List<Sach>
            {
                new Sach { MaSach = "BK0001", TenSach = "Vật lý vui", NamXB = 2015, MaLoai = khoaHoc, HinhAnh = "vatly.jpg" },
                new Sach { MaSach = "BK0002", TenSach = "Hóa học quanh ta", NamXB = 2016, MaLoai = khoaHoc },
                new Sach { MaSach = "BK0003", TenSach = "Dế Mèn Phiêu Lưu Ký", NamXB = 2010, MaLoai = vanHoc },
                new Sach { MaSach = "BK0004", TenSach = "Lão Hạc", NamXB = 2009, MaLoai = vanHoc },
                new Sach { MaSach = "BK0005", TenSach = "C# Căn Bản", NamXB = 2020, MaLoai = cntt },
                new Sach { MaSach = "BK0006", TenSach = "Entity Framework 6", NamXB = 2021, MaLoai = cntt },
                new Sach { MaSach = "BK0007", TenSach = "Kinh tế học cơ bản", NamXB = 2018, MaLoai = kinhTe },
                new Sach { MaSach = "BK0008", TenSach = "Nghệ thuật đàm phán", NamXB = 2017, MaLoai = knSong },
                new Sach { MaSach = "BK0009", TenSach = "Tối giản hóa", NamXB = 2019, MaLoai = knSong },
                new Sach { MaSach = "BK0010", TenSach = "Quản trị tài chính", NamXB = 2022, MaLoai = kinhTe }
            };
            foreach (var b in books)
                db.Saches.AddOrUpdate(x => x.MaSach, b);

            db.SaveChanges();
        }
    }
}
