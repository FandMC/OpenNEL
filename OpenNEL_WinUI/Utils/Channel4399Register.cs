/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using OpenNEL.Core.Utils;
using Codexus.Development.SDK.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Codexus.Cipher.Entities.Pc4399;
using Codexus.Cipher.Utils.Http;

namespace OpenNEL_WinUI.Utils;

public class Channel4399Register : IDisposable
{
  private readonly 
  HttpWrapper _register = new HttpWrapper("https://ptlogin.4399.com", handler: new HttpClientHandler
  {
    AllowAutoRedirect = true
  });

  public void Dispose()
  {
    _register.Dispose();
    GC.SuppressFinalize(this);
  }

  public async Task<Entity4399Account> RegisterAsync(
    Func<string, Task<string>> inputCaptchaAsync,
    Func<IdCard> idCardFunc)
  {
    string account = "OpenNEL" + RandomUtil.GetRandomString(4);
    string password = RandomUtil.GetRandomString(8);
    string captchaId = RandomUtil.GenerateSessionId();
    string captchaUrl = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaId;
    
    var (captcha, usedUrl) = await CaptchaRecognitionService.RecognizeWithRetryAsync(captchaUrl, inputCaptchaAsync);
    
    var usedCaptchaId = ExtractCaptchaId(usedUrl) ?? captchaId;
    
    IdCard idCard = idCardFunc();
    HttpResponseMessage async = await _register.GetAsync(BuildRegisterUrl(usedCaptchaId, captcha, account, password, idCard.IdNumber, idCard.Name));
    if (!async.IsSuccessStatusCode)
      throw new Exception("Status Code:" + async.StatusCode);
    EnsureRegisterSuccess(await async.Content.ReadAsStringAsync());
    
    _ = ReportCaptchaSuccessAsync(usedUrl, captcha);
    
    Entity4399Account entity4399Account = new Entity4399Account
    {
      Account = account,
      Password = password
    };
    return entity4399Account;
  }

  private static string? ExtractCaptchaId(string url)
  {
    try
    {
      var uri = new Uri(url);
      var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
      return query["captchaId"];
    }
    catch
    {
      return null;
    }
  }

  private static async Task ReportCaptchaSuccessAsync(string captchaUrl, string captchaText)
  {
    try
    {
      using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
      var imageBytes = await http.GetByteArrayAsync(captchaUrl);
      var base64 = Convert.ToBase64String(imageBytes);
      await CaptchaRecognitionService.ReportSuccessAsync(base64, captchaText);
    }
    catch { }
  }

  private static string BuildRegisterUrl(
    string captchaId,
    string captcha,
    string account,
    string password,
    string idCard,
    string name)
  {
    return "/ptlogin/register.do?" + new ParameterBuilder().Append("postLoginHandler", "default").Append("displayMode", "popup").Append("appId", "www_home").Append("gameId", "").Append("cid", "").Append("externalLogin", "qq").Append("aid", "").Append("ref", "").Append("css", "").Append("redirectUrl", "").Append("regMode", "reg_normal").Append("sessionId", captchaId).Append("regIdcard", "true").Append("noEmail", "false").Append("crossDomainIFrame", "").Append("crossDomainUrl", "").Append("mainDivId", "popup_reg_div").Append("showRegInfo", "true").Append("includeFcmInfo", "false").Append("expandFcmInput", "true").Append("fcmFakeValidate", "true").Append("userNameLabel", "4399用户名").Append("username", account).Append(nameof (password), password).Append("realname", name).Append("idcard", idCard).Append("email", RandomUtil.GetRandomString(10, "0123456789") + "@qq.com").Append("reg_eula_agree", "on").Append("inputCaptcha", captcha).FormUrlEncode();
  }

  private static void EnsureRegisterSuccess(string content)
  {
    if (content.Contains("验证码错误"))
      throw new Exception("Captcha Invalid");
    if (content.Contains("用户名已被注册"))
      throw new Exception("Account has been registered");
    if (!content.Contains("请一定记住您注册的用户名和密码"))
      throw new Exception("Unknown error");
  }

  public static string GenerateRandomIdCard()
  {
    string idCard = $"110108{GetRandomDate("19700101", "20041231")}{RandomUtil.GetRandomString(3, "0123456789")}";
    return idCard + GetIdCardLastCode(idCard);
  }

  public static string GenerateChineseName()
  {
    ReadOnlySpan<char> randomString = (ReadOnlySpan<char>) RandomUtil.GetRandomString(1, "李王张刘陈杨赵黄周吴徐孙胡朱高林何郭马罗梁宋郑谢韩唐冯于董萧程曹袁邓许傅沈曾彭吕苏卢蒋蔡贾丁魏薛叶阎余潘杜戴夏钟汪田任姜范方石姚谭廖邹熊金陆郝孔白崔康毛邱秦江史顾侯邵孟龙万段漕钱汤尹黎易常武乔贺赖龚文");
    char chineseCharacter1 = GenerateChineseCharacter();
    ReadOnlySpan<char> readOnlySpan1 = new ReadOnlySpan<char>(ref chineseCharacter1);
    char chineseCharacter2 = GenerateChineseCharacter();
    ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(ref chineseCharacter2);
    return randomString.ToString() + readOnlySpan1.ToString() + readOnlySpan2.ToString();
  }

  private static char GenerateChineseCharacter() => (char) Random.Shared.Next(19968, 40870);

  private static string GetRandomDate(string startDate, string endDate)
  {
    DateTime exact = DateTime.ParseExact(startDate, "yyyyMMdd", CultureInfo.InvariantCulture);
    int days = (DateTime.ParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture) - exact).Days;
    return exact.AddDays(Random.Shared.Next(days)).ToString("yyyyMMdd");
  }

  private static string GetIdCardLastCode(string idCard)
  {
    int[] factors =
    {
      7,
      9,
      10,
      5,
      8,
      4,
      2,
      1,
      6,
      3,
      7,
      9,
      10,
      5,
      8,
      4,
      2
    };
    return new string[]
    {
      "1",
      "0",
      "X",
      "9",
      "8",
      "7",
      "6",
      "5",
      "4",
      "3",
      "2"
    }[idCard.Take(17).Select((Func<char, int, int>) ((c, i) => ((int) c - 48 /*0x30*/) * factors[i])).Sum() % 11];
  }
}
