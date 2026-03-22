using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Models.Report;

namespace UrbanEcho.Reporting
{
    //Parts of this tutorial used
    //https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli

    public class ReportContext : DbContext
    {
        public DbSet<Report> Reports { get; set; }

        public string DbPath { get; }

        public ReportContext()
        {
            string path = Path.GetFullPath(System.AppContext.BaseDirectory);
            DbPath = System.IO.Path.Join(path, "report.db");
            try
            {
                this.Database.Migrate();
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Error creating report database {ex.Message}"));
            }
        }

        // The following configures EF to create a Sqlite database file in the
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}