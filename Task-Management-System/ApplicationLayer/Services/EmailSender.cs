using Contract.DTOs;
using Contract.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private (string host, int port, bool enableSsl, string senderEmail, string displayName, string password, int timeoutMs) GetSmtpSettings()
        {
            var host = _configuration["SmtpSettings:Host"];
            if (string.IsNullOrWhiteSpace(host)) host = "smtp.gmail.com";

            var port = _configuration.GetValue<int>("SmtpSettings:Port", 587);
            if (port <= 0) port = 587;

            var enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl", true);

            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail)) senderEmail = "giftcardmessenger@gmail.com";

            var displayName = _configuration["SmtpSettings:SenderDisplayName"];
            if (string.IsNullOrWhiteSpace(displayName)) displayName = "Altensor TMS Bildiriş Sistemi";

            var password = _configuration["SmtpSettings:Password"];
            if (string.IsNullOrWhiteSpace(password)) password = "gkvc ivgx eglv gnlf";

            var timeoutMs = _configuration.GetValue<int>("SmtpSettings:TimeoutMs", 15000);
            if (timeoutMs <= 0) timeoutMs = 15000;

            return (host, port, enableSsl, senderEmail, displayName, password, timeoutMs);
        }

        private SmtpClient CreateSmtpClient()
        {
            var (host, port, enableSsl, senderEmail, _, password, timeoutMs) = GetSmtpSettings();

            return new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = enableSsl,
                Timeout = timeoutMs
            };
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var (_, _, _, senderEmail, displayName, _, _) = GetSmtpSettings();
            var fromAddress = new MailAddress(senderEmail, displayName);
            var toAddress = new MailAddress(toEmail);

            using var smtp = CreateSmtpClient();

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Şifrə Sıfırlama Kodu</title>
</head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background-color:#f1f5f9; padding:40px 10px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width:520px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 10px 25px rgba(15,23,42,0.06); border:1px solid #e2e8f0;' cellpadding='0' cellspacing='0'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding:32px 30px; text-align:center;'>
                            <div style='display:inline-block; padding:6px 14px; background:rgba(56,189,248,0.15); border:1px solid rgba(56,189,248,0.3); border-radius:9999px; font-size:12px; font-weight:700; color:#38bdf8; letter-spacing:1.5px; text-transform:uppercase;'>
                                ALTENSOR TMS
                            </div>
                            <h1 style='margin:16px 0 0 0; color:#ffffff; font-size:22px; font-weight:700;'>Şifrə Sıfırlama Təsdiqi</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding:36px 32px 28px 32px;'>
                            <p style='margin:0 0 16px 0; color:#334155; font-size:15px; line-height:1.6;'>
                                Hörmətli istifadəçi,<br>
                                Hesabınızın şifrəsini bərpa etmək üçün təsdiq kodu sorğulanmışdır. Aşağıdakı təhlükəsizlik kodundan istifadə edin:
                            </p>
                            <div style='background-color:#f8fafc; border:2px dashed #cbd5e1; border-radius:12px; padding:20px; text-align:center; margin:24px 0;'>
                                <div style='font-size:32px; font-weight:800; letter-spacing:8px; color:#0284c7; font-family:monospace;'>
                                    {otp}
                                </div>
                                <div style='margin-top:8px; font-size:12px; color:#64748b; font-weight:500;'>
                                    ⏳ Kod 5 dəqiqə ərzində etibarlıdır
                                </div>
                            </div>
                            <p style='margin:0; color:#64748b; font-size:13px; line-height:1.6;'>
                                Əgər bu sorğunu siz göndərməmisinizsə, zəhmət olmasa bu məktubu nəzərə almayın və ya dərhal sistem administratoru ilə əlaqə saxlayın.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc; border-top:1px solid #e2e8f0; padding:20px 30px; text-align:center;'>
                            <p style='margin:0; font-size:12px; color:#94a3b8;'>
                                © {DateTime.UtcNow.Year} Altensor Task Management System. Bütün hüquqlar qorunur.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Altensor TMS — Şifrə Sıfırlama Kodu",
                IsBodyHtml = true,
                Body = body
            };

            await smtp.SendMailAsync(message);
        }

        public async Task SendOverdueTasksEmailAsync(string toEmail, List<OverdueTaskEmailItem> overdueTasks)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || overdueTasks == null || overdueTasks.Count == 0)
                return;

            var (_, _, _, senderEmail, displayName, _, _) = GetSmtpSettings();
            var fromAddress = new MailAddress(senderEmail, displayName);
            var toAddress = new MailAddress(toEmail);

            using var smtp = CreateSmtpClient();

            int urgentHighCount = overdueTasks.Count(t => t.Priority.Equals("Urgent", StringComparison.OrdinalIgnoreCase) ||
                                                          t.Priority.Equals("High", StringComparison.OrdinalIgnoreCase));
            int totalCount = overdueTasks.Count;
            var reportDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC");

            var sb = new StringBuilder();
            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Vaxtı Keçmiş Tapşırıqlar Hesabatı</title>
</head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background-color:#f1f5f9; padding:30px 10px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width:880px; background-color:#ffffff; border-radius:18px; overflow:hidden; box-shadow:0 12px 30px rgba(15,23,42,0.08); border:1px solid #e2e8f0;' cellpadding='0' cellspacing='0'>
                    
                    <!-- Header Banner -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding:32px 36px;'>
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td>
                                        <div style='display:inline-block; padding:5px 12px; background:rgba(56,189,248,0.15); border:1px solid rgba(56,189,248,0.3); border-radius:9999px; font-size:11px; font-weight:700; color:#38bdf8; letter-spacing:1.2px; text-transform:uppercase;'>
                                            ALTENSOR TMS • BİLDİRİŞ MƏRKƏZİ
                                        </div>
                                        <h1 style='margin:14px 0 6px 0; color:#ffffff; font-size:24px; font-weight:800; letter-spacing:-0.5px;'>
                                            ⚠️ Vaxtı Keçmiş Tapşırıqlar Hesabatı
                                        </h1>
                                        <p style='margin:0; color:#94a3b8; font-size:14px;'>
                                            Sistemdə vaxtı (deadline) bitmiş, lakin hələ tamamlanmamış tapşırıqların xülasəsi.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Stat Highlight Cards -->
                    <tr>
                        <td style='padding:28px 36px 12px 36px; background-color:#f8fafc; border-bottom:1px solid #e2e8f0;'>
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <!-- Card 1 -->
                                    <td width='32%' style='background:#ffffff; border:1px solid #e2e8f0; border-radius:12px; padding:16px; vertical-align:top;'>
                                        <div style='font-size:12px; font-weight:600; color:#64748b; text-transform:uppercase; letter-spacing:0.5px;'>Ümumi Gecikən</div>
                                        <div style='font-size:26px; font-weight:800; color:#0f172a; margin-top:4px;'>{totalCount} <span style='font-size:13px; font-weight:500; color:#64748b;'>tapşırıq</span></div>
                                    </td>
                                    <td width='2%'></td>
                                    <!-- Card 2 -->
                                    <td width='32%' style='background:#ffffff; border:1px solid #fee2e2; border-radius:12px; padding:16px; vertical-align:top;'>
                                        <div style='font-size:12px; font-weight:600; color:#ef4444; text-transform:uppercase; letter-spacing:0.5px;'>Təcili / Yüksək</div>
                                        <div style='font-size:26px; font-weight:800; color:#dc2626; margin-top:4px;'>{urgentHighCount} <span style='font-size:13px; font-weight:500; color:#ef4444;'>tapşırıq</span></div>
                                    </td>
                                    <td width='2%'></td>
                                    <!-- Card 3 -->
                                    <td width='32%' style='background:#ffffff; border:1px solid #e2e8f0; border-radius:12px; padding:16px; vertical-align:top;'>
                                        <div style='font-size:12px; font-weight:600; color:#64748b; text-transform:uppercase; letter-spacing:0.5px;'>Hesabat Vaxtı</div>
                                        <div style='font-size:13px; font-weight:700; color:#334155; margin-top:8px;'>{reportDate}</div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Table Content -->
                    <tr>
                        <td style='padding:28px 36px;'>
                            <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:separate; border-spacing:0; border:1px solid #e2e8f0; border-radius:12px; overflow:hidden;'>
                                <thead>
                                    <tr style='background-color:#f8fafc;'>
                                        <th style='padding:12px 14px; text-align:left; font-size:11px; font-weight:700; color:#475569; text-transform:uppercase; letter-spacing:0.5px; border-bottom:1px solid #e2e8f0;'>Tapşırıq</th>
                                        <th style='padding:12px 14px; text-align:left; font-size:11px; font-weight:700; color:#475569; text-transform:uppercase; letter-spacing:0.5px; border-bottom:1px solid #e2e8f0;'>İcraçı</th>
                                        <th style='padding:12px 14px; text-align:left; font-size:11px; font-weight:700; color:#475569; text-transform:uppercase; letter-spacing:0.5px; border-bottom:1px solid #e2e8f0;'>Layihə / Mərhələ</th>
                                        <th style='padding:12px 14px; text-align:center; font-size:11px; font-weight:700; color:#475569; text-transform:uppercase; letter-spacing:0.5px; border-bottom:1px solid #e2e8f0;'>Prioritet</th>
                                        <th style='padding:12px 14px; text-align:center; font-size:11px; font-weight:700; color:#475569; text-transform:uppercase; letter-spacing:0.5px; border-bottom:1px solid #e2e8f0;'>Deadline</th>
                                        <th style='padding:12px 14px; text-align:center; font-size:11px; font-weight:700; color:#475569; text-transform:uppercase; letter-spacing:0.5px; border-bottom:1px solid #e2e8f0;'>Gecikmə</th>
                                    </tr>
                                </thead>
                                <tbody>");

            int rowIndex = 0;
            foreach (var task in overdueTasks)
            {
                rowIndex++;
                var rowBg = rowIndex % 2 == 0 ? "#fcfdfe" : "#ffffff";

                var (priorityText, priorityBadgeStyle) = task.Priority?.ToLowerInvariant() switch
                {
                    "urgent" => ("Təcili", "background-color:#fee2e2; color:#b91c1c; border:1px solid #fecaca;"),
                    "high" => ("Yüksək", "background-color:#ffedd5; color:#c2410c; border:1px solid #fed7aa;"),
                    "normal" => ("Normal", "background-color:#e0f2fe; color:#0369a1; border:1px solid #bae6fd;"),
                    _ => ("Aşağı", "background-color:#f1f5f9; color:#475569; border:1px solid #e2e8f0;")
                };

                var durationStr = !string.IsNullOrWhiteSpace(task.OverdueDuration)
                    ? task.OverdueDuration
                    : (task.DaysOverdue > 0 ? $"{task.DaysOverdue} gün" : "1 gün");

                var assignee = task.AssignedToUserName ?? task.AssignedToEmail ?? "Təyin edilməyib";
                var projectHierarchy = !string.IsNullOrWhiteSpace(task.ProjectName)
                    ? $"{task.ProjectName}{(string.IsNullOrWhiteSpace(task.LevelName) ? "" : " / " + task.LevelName)}"
                    : (task.DivisionName ?? "-");

                sb.Append($@"
                                    <tr style='background-color:{rowBg};'>
                                        <td style='padding:13px 14px; font-size:13px; font-weight:600; color:#0f172a; border-bottom:1px solid #f1f5f9; vertical-align:middle;'>
                                            {task.Title}
                                        </td>
                                        <td style='padding:13px 14px; font-size:13px; color:#334155; border-bottom:1px solid #f1f5f9; vertical-align:middle;'>
                                            {assignee}
                                        </td>
                                        <td style='padding:13px 14px; font-size:12px; color:#64748b; border-bottom:1px solid #f1f5f9; vertical-align:middle;'>
                                            {projectHierarchy}
                                        </td>
                                        <td style='padding:13px 14px; text-align:center; border-bottom:1px solid #f1f5f9; vertical-align:middle;'>
                                            <span style='display:inline-block; padding:3px 8px; border-radius:9999px; font-size:11px; font-weight:700; {priorityBadgeStyle}'>
                                                {priorityText}
                                            </span>
                                        </td>
                                        <td style='padding:13px 14px; text-align:center; font-size:12px; color:#475569; border-bottom:1px solid #f1f5f9; vertical-align:middle; white-space:nowrap;'>
                                            {task.Deadline:yyyy-MM-dd HH:mm}
                                        </td>
                                        <td style='padding:13px 14px; text-align:center; border-bottom:1px solid #f1f5f9; vertical-align:middle; white-space:nowrap;'>
                                            <span style='display:inline-block; padding:3px 10px; border-radius:6px; background-color:#fef2f2; border:1px solid #fecaca; color:#dc2626; font-size:12px; font-weight:700;'>
                                                ⏱️ {durationStr}
                                            </span>
                                        </td>
                                    </tr>");
            }

            sb.Append($@"
                                </tbody>
                            </table>

                            <div style='margin-top:24px; padding:14px 18px; background-color:#eff6ff; border:1px solid #dbeafe; border-radius:10px;'>
                                <p style='margin:0; font-size:13px; color:#1e40af; line-height:1.5;'>
                                    💡 <strong>Məlumat:</strong> Bu tapşırıqların cari vəziyyətini idarə etmək və ya icraçılarla əlaqə saxlamaq üçün Altensor TMS tətbiqində Task Board bölməsinə keçid edə bilərsiniz.
                                </p>
                            </div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc; border-top:1px solid #e2e8f0; padding:22px 36px; text-align:center;'>
                            <p style='margin:0 0 6px 0; font-size:12px; color:#64748b;'>
                                Bu avtomatik hesabat <strong>Altensor TMS (Task Management System)</strong> tərəfindən göndərilmişdir.
                            </p>
                            <p style='margin:0; font-size:11px; color:#94a3b8;'>
                                © {DateTime.UtcNow.Year} Altensor Platform. Bütün hüquqlar qorunur.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = $"⚠️ [TMS] Gecikmiş Tapşırıqlar Hesabatı ({overdueTasks.Count} tapşırıq)",
                IsBodyHtml = true,
                Body = sb.ToString()
            };

            await smtp.SendMailAsync(message);
        }
    }
}
