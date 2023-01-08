using MyJetWallet.Sdk.Service.Tools.JsonMaskConverter;
using Newtonsoft.Json;

namespace MyJetWallet.Sdk.Service.Test;

public class MyNameJsonMaskConverterTest
{
    public class MyUsers
    {
        public List<User> Users { get; set; } = new List<User>();
        public User Author { get; set; }
    }

    public class User
    {
        public string Id { get; set; }

        [JsonConverter(typeof(MyNameJsonMaskConverter))]
        public string UserName { get; set; }

        [JsonConverter(typeof(MyEmailJsonMaskConverter))]
        public string Email { get; set; }

        public UserToken Token { get; set; }
        
        [JsonConverter(typeof(MyCardJsonMaskConverter))]
        public string Card { get; set; }
    }

    public class UserToken
    {
        public string PublicToken { get; set; }

        [JsonConverter(typeof(MyDefaultJsonMaskConverter))]
        public string PrivateToken { get; set; }
    }

    public class TestLogMaskedAttributes
    {
        [Test]
        public void OneUseLogMaskedAttribute()
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = "Joe",
                Email = "joe@mailinator.com",
                Card = "6222020000000085",
                Token = new UserToken
                {
                    PublicToken = "Public:" + Guid.NewGuid().ToString("N"),
                    PrivateToken = "Private:" + Guid.NewGuid().ToString("N")
                }
            };
            var userMasked = JsonConvert.SerializeObject(user);
            var userFromJson = JsonConvert.DeserializeObject<User>(userMasked);
            Assert.IsTrue(userFromJson?.UserName == "J*e");
            Assert.IsTrue(userFromJson?.Email == "jo*************com");
            Assert.IsTrue(userFromJson?.Token.PrivateToken == "***");
            Assert.IsTrue(userFromJson?.Card == "622202***0085");
        }

        [Test]
        public void ArrayOfUsersLogMaskedAttribute()
        {
            var users = new MyUsers()
            {
                Users = new List<User>()
                {
                    new User()
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        UserName = null,
                        Email = null,
                        Card = null,
                        Token = null,
                    },
                    new User()
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        UserName = string.Empty,
                        Email = string.Empty,
                        Card = string.Empty,
                        Token = new UserToken
                        {
                            PublicToken = string.Empty,
                            PrivateToken = string.Empty
                        }
                    }
                },
                Author = new User
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserName = "Joe",
                    Email = "joe@mailinator.com",
                    Card = "6222020000000085",
                    Token = new UserToken
                    {
                        PublicToken = "Public:" + Guid.NewGuid().ToString("N"),
                        PrivateToken = "Private:" + Guid.NewGuid().ToString("N")
                    }
                }
            };
            var userMasked = JsonConvert.SerializeObject(users);
            var usersFromJson = JsonConvert.DeserializeObject<MyUsers>(userMasked);
            Assert.IsTrue(usersFromJson?.Author.UserName == "J*e");
            Assert.IsTrue(usersFromJson?.Author.Email == "jo*************com");
            Assert.IsTrue(usersFromJson?.Author.Token.PrivateToken == "***");
            Assert.IsTrue(usersFromJson?.Author.Card == "622202***0085");
        }
    }
}