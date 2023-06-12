using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NugetVTChallenge.Interfaces;
using NugetVTChallenge.Models;
using NugetVTChallenge.Models.Api;
using System.Data;
using System.Text;
using VTChallengeAWSApi.Services;
using VTChallengeAWSApi.Data;
using VTChallengeAWSApi.Helpers;
using static System.Net.Mime.MediaTypeNames;

namespace VTChallengeAWSApi.Repositories
{

    #region PROCEDURES

    #endregion

    public class RepositoryVtChallenge : IVtChallenge
    {

        private VTChallengeContext context;
        private IServiceValorant api;

        public RepositoryVtChallenge(VTChallengeContext context, IServiceValorant api)
        {
            this.context = context;
            this.api = api;
        }

        #region METHODS USERS
        public async Task<List<Usuario>> GetUsers()
        {
            var consulta = from data in this.context.Users
                           select data;
            return await consulta.ToListAsync();
        }

        public async Task<Usuario> FindUserAsync(string uid)
        {
            return await this.context.Users.FirstOrDefaultAsync(x => x.Uid == uid);
        }

        public async Task<Usuario> FindUserByNameAsync(string name)
        {
            return await this.context.Users.FirstOrDefaultAsync(x => x.Name == name);
        }


        public async Task<Usuario> LoginNamePasswordAsync(string username, string password)
        {
            Usuario user = await this.context.Users.FirstOrDefaultAsync(u => u.Name == username);
            if (user == null)
            {
                return null;
            }
            else
            {
                byte[] passUsuario = user.PassEncript;
                string salt = user.Salt;

                byte[] temp = HelperCryptography.EncryptPassword(password, salt);
                bool respuesta = HelperCryptography.CompareArrays(passUsuario, temp);

                if (respuesta == true)
                {
                    return user;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task RegisterUserAsync(string uid, string name, string tag, string email, string password, string imagesmall, string imagelarge, string rango)
        {
            Usuario user = new Usuario();

            user.Uid = uid;
            user.Name = name;
            user.Tag = tag;
            user.Email = email;
            user.Password = password;
            user.ImageSmall = imagesmall;
            user.ImageLarge = imagelarge;
            user.Rank = rango;
            user.Rol = "player";
            user.Salt = HelperCryptography.GenerateSalt();
            user.PassEncript = HelperCryptography.EncryptPassword(password, user.Salt);

            this.context.Users.Add(user);
            await this.context.SaveChangesAsync();
        }

        public async Task<int> GetTotalWinsAsync(string uid)
        {
            var victories = (from m in this.context.Matches
                             join r in this.context.Rounds on m.Rid equals r.Rid
                             where r.Name == "Final" &&
                                   ((m.Rred > m.Rblue) || (m.Rblue > m.Rred))
                             join tp in this.context.TournamentPlayers on r.Tid equals tp.Tid
                             where tp.Uid == uid
                             select r.Tid).Distinct().Count();

            return (int)victories;
        }

        public async Task UpdateProfileAsync(string uid)
        {
            DataApi data = null;
            UserApi userapi = await this.api.GetAccountUidAsync(uid);
            Usuario user = await this.FindUserAsync(uid);

            if (userapi != null)
            {
                data = userapi.Data;
                user.ImageLarge = data.Card.Large;
                user.ImageSmall = data.Card.Small;
                user.Rank = await this.api.GetRankAsync(user.Name, user.Tag);
            }
            await this.context.SaveChangesAsync();
        }
        #endregion

        #region METHODS TOURNAMENTS
        public async Task<List<TournamentPlayers>> GetPlayersTournament(int tid)
        {
            var consulta = from data in this.context.TournamentPlayers
                           where data.Tid == tid
                           select data;
            return await consulta.OrderBy(x => x.Team).ToListAsync();
        }

        public async Task<TournamentComplete> GetTournamentComplete(int tid)
        {
            return await this.context.TournamentCompletes.FirstOrDefaultAsync(z => z.Tid == tid);
        }

        public async Task<List<TournamentComplete>> GetTournaments()
        {
            var consulta = from data in this.context.TournamentCompletes
                           orderby data.DateInit descending
                           select data;

            return await consulta.ToListAsync();
        }

        public async Task<List<TournamentComplete>> GetTournamentCompletesFindAsync(string filtro, string rank)
        {
            var consulta = from data in this.context.TournamentCompletes
                           where data.Name.Contains(filtro) && (data.Rank.Contains("Unranked") || data.Rank.Contains(rank))
                           orderby data.DateInit descending
                           select data;

            return await consulta.ToListAsync();
        }

        public async Task<List<TournamentComplete>> GetTournamentsByRankAsync(string rank)
        {
            var consulta = from data in this.context.TournamentCompletes
                           where data.Rank.Contains(rank) || data.Rank.Contains("Unranked")
                           orderby data.DateInit descending
                           select data;

            return await consulta.ToListAsync();
        }

        public async Task<List<Round>> GetRounds(int tid)
        {
            var consulta = from data in this.context.Rounds
                           where data.Tid == tid
                           select data;
            return await consulta.ToListAsync();
        }

        public async Task<List<MatchRound>> GetMatchesTournament(int tid)
        {
            var consulta = from match in this.context.Matches
                           join round in this.context.Rounds on match.Rid equals round.Rid
                           where round.Tid == tid
                           select new MatchRound
                           {
                               Mid = match.Mid,
                               Tblue = match.Tblue,
                               Rblue = match.Rblue,
                               Tred = match.Tred,
                               Rred = match.Rred,
                               Date = match.Date,
                               Fase = round.Name
                           };
            return await consulta.ToListAsync();
        }

        public async Task<List<TournamentPlayers>> GetTournamentWinner(int tid)
        {
            var pamTid = new MySqlParameter("@VTID", MySqlDbType.VarChar)
            {
                Value = tid
            };

            var consulta = this.context.TournamentPlayers.FromSqlRaw("CALL SP_GETGANADOR_TOURNAMENT(@VTID)", pamTid);

            return await consulta.ToListAsync();
        }

        public async Task InscriptionPlayerTeamAle(int tid, string uid)
        {
            var tournament = await this.context.Tournaments.FirstOrDefaultAsync(x => x.Tid == tid);
            int numEquipos = (tournament.Players / 5);

            List<int> equiposTournament = Enumerable.Range(1, numEquipos).ToList();

            var teamComplete = this.context.TournamentPlayers.Where(tp => tp.Tid == tid)
                                                                   .GroupBy(tp => tp.Team)
                                                                   .Where(g => g.Count() == 5)
                                                                   .Select(g => g.Key);
            List<int> listTeamsComplete = await teamComplete.ToListAsync();

            //LISTA DE LOS EQUIPOS DISPONIBLES
            List<int> listEquiposDisponibles = equiposTournament.Except(listTeamsComplete).Concat(listTeamsComplete.Except(equiposTournament)).ToList();

            //ESCOGER UN EQUIPO RANDOM
            Random random = new Random();
            int indiceAleatorio = random.Next(listEquiposDisponibles.Count);
            int team = listEquiposDisponibles[indiceAleatorio];

            MySqlParameter[] pams = new MySqlParameter[] {
                    new MySqlParameter("@VTID", tid),
                    new MySqlParameter("@VUID", uid),
                    new MySqlParameter("@VTEAM", team)
            };

            await this.context.Database.ExecuteSqlRawAsync("CALL SP_INSCRIPTION_PLAYER_TEAMALE(@VTID,@VUID,@VTEAM)", pams);
        }

        public async Task<bool> ValidateInscription(int tid, string uid)
        {
            var consulta = await this.context.TournamentPlayers.FirstOrDefaultAsync(z => z.Tid == tid && z.Uid == uid);

            if (consulta != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<List<TournamentComplete>> GetTournamentsUser(string name)
        {
            var consulta = from data in this.context.TournamentCompletes
                           where data.Organizator == name
                           orderby data.DateInit descending
                           select data;
            return await consulta.ToListAsync();
        }


        public async Task<List<TournamentComplete>> GetTournamentsUserFindAsync(string name, string filtro)
        {
            var consulta = from data in this.context.TournamentCompletes
                           where data.Name.Contains(filtro) && data.Organizator == name
                           select data;

            return await consulta.ToListAsync();
        }

        public async Task DeleteTournament(int tid, string uid)
        {
            string sql = "CALL SP_DELETE_TOURNAMENT(@VTID)";
            MySqlParameter pamTid = new MySqlParameter("@VTID", tid);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamTid);
        }

        public async Task<Match> FindMatchAsync(int mid)
        {
            return await this.context.Matches.FirstOrDefaultAsync(x => x.Mid == mid);
        }


        public async Task DeleteUserTournamentAsync(int tid, string uid)
        {
            string sql = "CALL SP_DELETE_PLAYER_TOURNAMENT(@VTID, @VUID)";
            MySqlParameter pamTid = new MySqlParameter("@VTID", tid);
            MySqlParameter pamUid = new MySqlParameter("@VUID", uid);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamTid, pamUid);
        }

        public async Task UpdateMatchesTournamentAsync(int mid, int rblue, int rred)
        {
            Match match = await this.FindMatchAsync(mid);
            match.Rblue = rblue;
            match.Rred = rred;
            await this.context.SaveChangesAsync();
        }

        public async Task<int> TotalMatchesRoundWinner(int rid) {
            int prueba = 1;
            var consulta = await this.context.Matches.Where(m => m.Rid == rid && (m.Rblue != null && m.Rred != null && m.Rblue != m.Rred)).CountAsync();
            return (int)consulta;
        }

        public async Task<int> TotalMatchesRound(int rid)
        {
            var consulta = await this.context.Matches.Where(m => m.Rid == rid).CountAsync();
            return (int)consulta;
        }

        public async Task InsertMatchesNextRoundAsync(int rid)
        {
            int next_round;
            int i = 1;
            int next_mid;
            DateTime date;

            next_round = rid + 1;
            date = await this.context.Rounds.Where(r => r.Rid == next_round).Select(r => r.Date).FirstOrDefaultAsync();

            var winners = await (from m in this.context.Matches
                                 where m.Rid == rid && m.Rblue > m.Rred
                                 select m.Tblue)
            .Union(from m in this.context.Matches
                   where m.Rid == rid && m.Rblue < m.Rred
                   select m.Tred).ToListAsync();

            while (i <= winners.Count / 2)
            {
                next_mid = this.context.Matches.Max(m => m.Mid) + 1;

                int W1 = winners[(2 * i) - 2];
                int W2 = winners[(2 * i) - 1];

                this.context.Matches.Add(new Match
                {
                    Mid = next_mid,
                    Tblue = W1,
                    Tred = W2,
                    Rblue = 0,
                    Rred = 0,
                    Date = date,
                    Rid = next_round
                });

                context.SaveChanges();

                i++;
            }
        }

        public int GetMaxIdTournament()
        {
            return this.context.Tournaments.Max(t => t.Tid);
        }

        public int GetMinIdRoundTournament(int tid)
        {
            return this.context.Rounds
                    .Where(r => r.Tid == tid && r.Rid == this.context.Rounds.Where(r2 => r2.Tid == tid).Min(r2 => r2.Rid))
                    .Select(r => r.Rid)
                    .FirstOrDefault();
        }

        public async Task InsertTournamentAsync(int tid, string name, string rank, DateTime dateinit, string description, int pid, int players, string organizator, string image)
        {
            Tournament tournament = new Tournament()
            {
                Tid = tid,
                Name = name,
                Rank = rank,
                DateInit = dateinit,
                Description = description,
                Platform = pid,
                Players = players,
                Organizator = organizator,
                Image = image
            };
            this.context.Tournaments.Add(tournament);
            await this.context.SaveChangesAsync();
        }

        public async Task InsertRoundAsync(string name, DateTime date, int tid)
        {
            Round round = new Round()
            {
                Rid = await this.context.Rounds.MaxAsync(r => r.Rid) + 1,
                Name = name,
                Date = date,
                Tid = tid
            };
            this.context.Rounds.Add(round);
            await this.context.SaveChangesAsync();
        }

        public async Task InsertMatchAsync(int tblue, int tred, DateTime time, int rid)
        {
            Match match = new Match()
            {
                Mid = await this.context.Matches.MaxAsync(m => m.Mid) + 1,
                Tblue = tblue,
                Tred = tred,
                Rblue = 0,
                Rred = 0,
                Date = time,
                Rid = rid
            };
            this.context.Matches.Add(match);
            await this.context.SaveChangesAsync();
        }

        public async Task<Round> FindRoundAsync(int rid)
        {
            return await this.context.Rounds.FirstOrDefaultAsync(x => x.Rid == rid);
        }

        public async Task<List<Trajectory>> GetTrajectoryUserAsync(string uid)
        {
            var query = from user in this.context.Users
                        join tournamentPlayer in this.context.TournamentPlayers on user.Uid equals tournamentPlayer.Uid
                        join tournament in this.context.Tournaments on tournamentPlayer.Tid equals tournament.Tid
                        where user.Uid == uid
                        select new
                        {
                            Tournament = tournament,
                            TournamentPlayer = tournamentPlayer
                        };

            var tournaments = await query.ToListAsync();
            var trajectoryList = new List<Trajectory>();

            foreach (var item in tournaments)
            {
                var tournament = item.Tournament;
                var tournamentPlayer = item.TournamentPlayer;

                var finalMatch = this.context.Matches
                    .Join(
                        this.context.Rounds,
                        match => match.Rid,
                        round => round.Rid,
                        (match, round) => new { Match = match, Round = round }
                    )
                    .FirstOrDefault(joinResult => joinResult.Round.Tid == tournament.Tid && joinResult.Round.Name == "Final");

                var isWinner = finalMatch != null && ((finalMatch.Match.Tblue == tournamentPlayer.Team && finalMatch.Match.Rblue == 3) ||
                                                      (finalMatch.Match.Tred == tournamentPlayer.Team && finalMatch.Match.Rred == 3));
                var isBelowThree = finalMatch == null || (finalMatch.Match.Rblue < 3 && finalMatch.Match.Rred < 3);

                string imageOrganizator = this.context.Users.FirstOrDefault(x => x.Uid == tournament.Organizator).ImageSmall;

                var trajectory = new Trajectory
                {
                    Tid = tournament.Tid,
                    TournamentName = tournament.Name,
                    TournamentImage = tournament.Image,
                    TournamentRank = tournament.Rank,
                    TournamentDateInit = tournament.DateInit,
                    OrganizatorImage = imageOrganizator,
                    Winner = isWinner ? WinnerStatus.True : (isBelowThree ? WinnerStatus.Ongoing : WinnerStatus.False)
                };

                trajectoryList.Add(trajectory);
            }

            return trajectoryList;
        }

        public async Task<List<Weapon>> GetWeaponsAsync()
        {
            return await this.api.GetWeaponsAsync();
        }
        public async Task<Skin> GetSkinAsync(string skinid)
        {
            return await this.api.GetSkinById(skinid);
        }

        public async Task<Weapon> GetWeaponBySkinAsync(string skinid)
        {
            return await this.api.GetWeaponBySkinUuid(skinid);
        }

        #endregion
    }
}
