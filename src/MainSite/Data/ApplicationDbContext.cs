#region Usings

using MainSite.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

#endregion

namespace MainSite.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        /// <summary>
        /// Gets or sets the DbSet for Companies.
        /// </summary>
        public DbSet<Company> Companies { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Products.
        /// </summary>
        public DbSet<Product> Products { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Company Product joins.
        /// </summary>
        public DbSet<ProductLink> ProductLinks { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Contacts.
        /// </summary>
        public DbSet<Contact> Contacts { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Documentations.
        /// </summary>
        public DbSet<Documentation> Documentations { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Categories.
        /// </summary>
        public DbSet<Category> Categories { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Machines.
        /// </summary>
        public DbSet<Machine> Machines { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for MachineCredentials.
        /// </summary>
        public DbSet<MachineCredentials> MachineCredentials { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Vpns.
        /// </summary>
        public DbSet<Vpn> Vpns { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for VpnCredentials
        /// </summary>
        public DbSet<VpnCredentials> VpnCredentials { get; set; }
        /// <summary>
        /// Gets or sets the DbSet for Addresses
        /// </summary>
        public DbSet<Address> Addresses { get; set; }
        
        /// <param name="builder"></param>

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}