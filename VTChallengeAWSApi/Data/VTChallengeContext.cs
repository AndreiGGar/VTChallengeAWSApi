using Microsoft.EntityFrameworkCore;
using NugetVTChallenge.Models;

namespace VTChallengeAWSApi.Data {
    public class VTChallengeContext : DbContext {
        public VTChallengeContext(DbContextOptions<VTChallengeContext> options) : base(options) { }

        #region TABLAS
        public DbSet<Usuario> Users { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        #endregion

        #region VISTAS
        public DbSet<TournamentComplete> TournamentCompletes { get; set; }
        public DbSet<TournamentPlayers> TournamentPlayers { get; set; }
        #endregion 
    }
}
