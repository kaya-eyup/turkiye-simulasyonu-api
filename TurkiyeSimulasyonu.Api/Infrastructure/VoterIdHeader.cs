namespace TurkiyeSimulasyonu.Api.Infrastructure;

// v1'de giriş yok: tarayıcı kendi ürettiği kimliği bu başlıkla gönderir (anket ve öğe oyları).
// Giriş gelince yerini Authorization başlığı alır; gövdeler değişmez.
public static class VoterIdHeader
{
    public const string Name = "X-Voter-Id";
}