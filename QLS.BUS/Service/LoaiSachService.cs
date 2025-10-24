using QLS.DAL;
using QLS.DAL.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QLS.BUS
{
    public class LoaiSachService
    {
        public List<LoaiSach> GetAll()
        {
            using (var db = new QLSDbContext())
                return db.LoaiSaches.AsNoTracking().OrderBy(x => x.TenLoai).ToList();
        }
    }
}
