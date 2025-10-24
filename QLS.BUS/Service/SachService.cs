using QLS.DAL;
using QLS.DAL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QLS.BUS
{
    public class SachService
    {
        public sealed class SachListItem
        {
            public string MaSach { get; set; }
            public string TenSach { get; set; }
            public int NamXB { get; set; }
            public int MaLoai { get; set; }
            public string TenLoai { get; set; }
            public string HinhAnh { get; set; }
        }

        public List<SachListItem> GetAll()
        {
            using (var db = new QLSDbContext())
            {
                return db.Saches.AsNoTracking()
                         .Include(s => s.Loai)
                         .OrderByDescending(x => x.NamXB)
                         .Select(s => new SachListItem
                         {
                             MaSach = s.MaSach,
                             TenSach = s.TenSach,
                             NamXB = s.NamXB,
                             MaLoai = s.MaLoai,
                             TenLoai = s.Loai.TenLoai,
                             HinhAnh = s.HinhAnh
                         }).ToList();
            }
        }

        public List<SachListItem> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return GetAll();
            keyword = keyword.Trim();

            using (var db = new QLSDbContext())
            {
                return db.Saches.AsNoTracking()
                         .Include(s => s.Loai)
                         .Where(s => s.MaSach.Contains(keyword)
                                  || s.TenSach.Contains(keyword)
                                  || s.NamXB.ToString().Contains(keyword))
                         .OrderByDescending(x => x.NamXB)
                         .Select(s => new SachListItem
                         {
                             MaSach = s.MaSach,
                             TenSach = s.TenSach,
                             NamXB = s.NamXB,
                             MaLoai = s.MaLoai,
                             TenLoai = s.Loai.TenLoai,
                             HinhAnh = s.HinhAnh
                         }).ToList();
            }
        }

        public void AddOrUpdate(Sach model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(model.MaSach) ||
                string.IsNullOrWhiteSpace(model.TenSach))
                throw new ArgumentException("Vui lòng nhập đầy đủ thông tin sách!");

            if (model.MaSach.Length != 6)
                throw new ArgumentException("Mã sách phải đúng 6 ký tự.");

            using (var db = new QLSDbContext())
            {
                var existed = db.Saches.FirstOrDefault(x => x.MaSach == model.MaSach);
                if (existed == null)
                {
                    db.Saches.Add(model);
                }
                else
                {
                    existed.TenSach = model.TenSach;
                    existed.NamXB = model.NamXB;
                    existed.MaLoai = model.MaLoai;
                    existed.HinhAnh = model.HinhAnh;
                }
                db.SaveChanges();
            }
        }

        public bool Delete(string maSach)
        {
            using (var db = new QLSDbContext())
            {
                var s = db.Saches.FirstOrDefault(x => x.MaSach == maSach);
                if (s == null) return false;
                db.Saches.Remove(s);
                db.SaveChanges();
                return true;
            }
        }

        public Sach GetById(string maSach)
        {
            using (var db = new QLSDbContext())
                return db.Saches.Include(x => x.Loai).FirstOrDefault(x => x.MaSach == maSach);
        }
    }
}
