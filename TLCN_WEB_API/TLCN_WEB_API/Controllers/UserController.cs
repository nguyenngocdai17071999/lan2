using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TLCN_WEB_API.Models;

namespace TLCN_WEB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "0ypBJAvuHDxyKu9sDI6xVtKpI6kkp9QEFqHS92dk",
            BasePath = "https://tlcn-1a9cf.firebaseio.com/"
        };
        private static string key = "TLCN";
        IFirebaseClient client;

        [HttpGet("GetAll")]

        //phương thức get dữ liệu từ firebase
        public IActionResult GetAll()
        {
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = client.Get("User");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
            var list = new List<User>();
            //danh sách tìm kiếm
            //var list2 = new List<User>();
            foreach (var item in data)
            {
                list.Add(JsonConvert.DeserializeObject<User>(((JProperty)item).Value.ToString()));
            }
            foreach (var item in list)
            {
                item.Password = Decrypt(item.Password);
            }
            return Ok(list);
        }

        [HttpGet("GetByID/{id:int}")]
        // phương thức get by id dữ liệu từ firebase 
        public async Task<IActionResult> GetByID(int id)
        {
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = client.Get("User");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
            var list = new List<User>();
            //danh sách tìm kiếm

            foreach (var item in data)
            {

                list.Add(JsonConvert.DeserializeObject<User>(((JProperty)item).Value.ToString()));
            }
            var list2 = new List<User>();
            foreach (var item in list)
            {
                if (item.UserID == id)
                    list2.Add(item);
            }
            foreach (var item in list)
            {
                item.Password = Decrypt(item.Password);
            }
            return Ok(list2);
        }

        [HttpPost("EditByID/{id:int}")]
        //thay đổi thông tin đã có trên firebase theo id
        public IActionResult EditByID(int id, [FromBody] User user)
        {

            try
            {
                AddbyidToFireBase(id, user);
                return Ok(new[] { "ok" });
            }
            catch
            {
                return Ok(new[] { "not ok" });
            }
        }


        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser([FromBody] User user)
        {
            string err = "";
            try
            {
                if (kiemtraEmail(user.Email) == false)
                {
                    AddToFireBase(user);
                    err = "Đăng ký thành công";

                }
                else
                {
                    err = "Email đã tồn tại";
                }

            }
            catch
            {
                err = "Lỗi rồi";
            }
            return Ok(new[] { err });

        }
        //Hàm login
        [HttpPost("Login")]
        public IActionResult Login([FromBody] Login user)
        {
            //get list user
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = client.Get("User");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
            var list = new List<User>();
            string err = "";
            foreach (var item in data)
            {
                list.Add(JsonConvert.DeserializeObject<User>(((JProperty)item).Value.ToString()));
            }
            foreach (var item in list)
            {
                if (item.Email == user.UserName && item.Password == Encrypt(user.PassWord))
                {

                    err = "thanh cong";
                    return Ok(item);
                    break;
                }
                else
                {
                    err = "that bai";
                }
            }
            return Ok(new[] { err });
        }


        [HttpPost("ForgetPass")]
        public IActionResult ForgetPass([FromBody] User user)
        {
            //ForGetPassWord forGetPassWord = new ForGetPassWord();
            //int code;
            //DateTime date;
            if (kiemtraEmail(user.Email) == true)
            {

                Random a = new Random();
                int code = a.Next(100000, 999999);
                DateTime date = DateTime.Now;
                //forGetPassWord.MaCode = code;
                //forGetPassWord.Date = date;
                ////Gửi email
                var messenge = new MimeMessage();
                messenge.From.Add(new MailboxAddress("Test Project", "nguyenngocdai17071999@gmail.com"));
                messenge.To.Add(new MailboxAddress("naren", user.Email));
                messenge.Subject = "hello";
                messenge.Body = new TextPart("plain")
                {
                    Text = "Code ResetPass cua ban la: " + code + ""
                };

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate("nguyenngocdai17071999@gmail.com", "conyeume");
                    client.Send(messenge);
                    client.Disconnect(true);
                }
                return Ok(new[] { "code: " + code + "", "date: " + date + "" });
            }

            return Ok(new[] { "khong co email" });

        }
        //resetpass theo gmail
        [HttpPost("ResetPass")]
        public IActionResult ResetPass([FromBody] User user)
        {
            //ForGetPassWord forGetPassWord = new ForGetPassWord();
            //int code;
            //DateTime date;
            if (kiemtraEmail(user.Email) == true)
            {
                User resetPassUser = new User();
                client = new FireSharp.FirebaseClient(config);
                FirebaseResponse response = client.Get("User");
                dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
                var list = new List<User>();
                string err = "";
                foreach (var item in data)
                {
                    list.Add(JsonConvert.DeserializeObject<User>(((JProperty)item).Value.ToString()));
                }
                foreach (var item in list)
                {
                    if (item.Email == user.Email)
                    {
                        resetPassUser = item;
                        break;
                    }
                }
                resetPassUser.Password = user.Password;
                EditPassBYID(resetPassUser.UserID, resetPassUser);
                return Ok(new[] { "Resetthanhcong" });

            }

            return Ok(new[] { "khong co email" });

        }



        //tim ra ID tự động của user bằng cách tăng dần từ 1 nếu đã có số rồi thì lấy số tiếp theo cho đến hết chuổi thì lấy số cuối cùng.
        // vd 1 2 3 thì get id sẽ ra 4
        // vd 1 3 4 thì get id sẽ ra 2
        private int GetID()
        {
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = client.Get("User");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
            var list = new List<User>();
            foreach (var item in data)
            {
                list.Add(JsonConvert.DeserializeObject<User>(((JProperty)item).Value.ToString()));
            }
            int i = 1;
            while (1 == 1)
            {
                int dem = 0;
                foreach (var item in list)
                {
                    if (item.UserID == i)
                        dem++;
                }
                if (dem == 0)
                    return i;
                i++;
            }
            return i;
        }
        // thêm dư liệu lên firebase
        private void AddToFireBase(User user)
        {
            client = new FireSharp.FirebaseClient(config);
            var data = user;
            //  PushResponse response = client.Push("User/", data);
            data.UserID = GetID();
            data.Password = Encrypt(data.Password);
            SetResponse setResponse = client.Set("User/" + data.UserID, data);
        }
        // Edit password by id
        private void EditPassBYID(int id, User user)
        {
            client = new FireSharp.FirebaseClient(config);
            var data = user;
            //  PushResponse response = client.Push("User/", data);
            data.UserID = id;
            data.Password = Encrypt(data.Password);
            SetResponse setResponse = client.Set("User/" + data.UserID, data);
        }
        //thêm dữ liệu lên firebase theo id
        private void AddbyidToFireBase(int id, User user)
        {
            client = new FireSharp.FirebaseClient(config);
            var data = user;
            //  PushResponse response = client.Push("User/", data);
            data.UserID = id;
            // data.Password = Encrypt(data.Password);
            SetResponse setResponse = client.Set("User/" + data.UserID, data);
        }
        // mã hóa dữ liệu MD5
        public static string Encrypt(string toEncrypt)
        {
            bool useHashing = true;
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        public static string Decrypt(string toDecrypt)
        {
            bool useHashing = true;
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        private bool kiemtraEmail(string email)
        {
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = client.Get("User");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
            var list = new List<User>();
            string err = "";
            foreach (var item in data)
            {
                list.Add(JsonConvert.DeserializeObject<User>(((JProperty)item).Value.ToString()));
            }
            foreach (var item in list)
            {
                if (item.Email == email)
                    return true;
            }
            return false;
        }
    }
}
