using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AshkanLedgerPro.Data;
namespace AshkanLedgerPro.Security;
public sealed class AppUser {
 public int Id {get;set;}
 public string Username {get;set;} = "";
 public string PasswordHash {get;set;} = "";
 public string Role {get;set;} = "Viewer";
 public bool IsActive {get;set;} = true;
}
public sealed class AuthService {
 public void Initialize(){using var db=new LedgerDb();db.Database.EnsureCreated();db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS AppUsers (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL, Role TEXT NOT NULL, IsActive INTEGER NOT NULL DEFAULT 1)");}
 public bool HasUsers(){using var db=new LedgerDb();return db.Set<AppUser>().Any();}
 public static string Hash(string password){if(password.Length<10)throw new ArgumentException("Password must have at least 10 characters.");byte[] salt=RandomNumberGenerator.GetBytes(16);byte[] hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,210000,HashAlgorithmName.SHA256,32);return $"pbkdf2-sha256:210000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";}
 public AppUser? Login(string username,string password){using var db=new LedgerDb();var u=db.Set<AppUser>().AsNoTracking().FirstOrDefault(x=>x.Username==username && x.IsActive);if(u==null)return null;try{var parts=u.PasswordHash.Split(':');if(parts.Length!=4 || parts[0]!="pbkdf2-sha256")return null;int rounds=int.Parse(parts[1]);if(rounds<100000 || rounds>1000000)return null;var expected=Convert.FromBase64String(parts[3]);var computed=Rfc2898DeriveBytes.Pbkdf2(password,Convert.FromBase64String(parts[2]),rounds,HashAlgorithmName.SHA256,expected.Length);return CryptographicOperations.FixedTimeEquals(expected,computed)?u:null;}catch{return null;}}
 public void CreateUser(string username,string password,string role){username=username.Trim();if(username.Length<3||username.Length>60||!new[]{"Admin","Accountant","Viewer"}.Contains(role))throw new ArgumentException("Invalid username or role.");using var db=new LedgerDb();db.Set<AppUser>().Add(new AppUser{Username=username,PasswordHash=Hash(password),Role=role});db.SaveChanges();}
 public bool ChangePassword(string username, string oldPassword, string newPassword)
 {
  if (Login(username, oldPassword) is null) return false;
  using var db = new LedgerDb();
  var user = db.Set<AppUser>().SingleOrDefault(x => x.Username == username && x.IsActive);
  if (user is null) return false;
  user.PasswordHash = Hash(newPassword);
  db.SaveChanges();
  return true;
 }
 public List<AppUser> Users(){using var db=new LedgerDb();return db.Set<AppUser>().AsNoTracking().OrderBy(x=>x.Username).ToList();}
}
