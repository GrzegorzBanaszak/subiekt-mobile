using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Infrastructure.Persistence.Entities;

namespace SubiektMobile.Infrastructure.Persistence.Configurations;

public sealed class TwTowarConfiguration : IEntityTypeConfiguration<TwTowar>
{
    public void Configure(EntityTypeBuilder<TwTowar> entity)
    {
        entity.ToTable("tw__Towar");

        entity.HasKey(towar => towar.Id)
            .HasName("PK_tw__Towar");

        entity.Property(towar => towar.Id).HasColumnName("tw_Id");
        entity.Property(towar => towar.Zablokowany).HasColumnName("tw_Zablokowany");
        entity.Property(towar => towar.Rodzaj).HasColumnName("tw_Rodzaj");
        entity.Property(towar => towar.Symbol).HasColumnName("tw_Symbol").HasMaxLength(20).IsUnicode(false);
        entity.Property(towar => towar.Nazwa).HasColumnName("tw_Nazwa").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Opis).HasColumnName("tw_Opis").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.IdVatSp).HasColumnName("tw_IdVatSp");
        entity.Property(towar => towar.IdVatZak).HasColumnName("tw_IdVatZak");
        entity.Property(towar => towar.JakPrzySp).HasColumnName("tw_JakPrzySp");
        entity.Property(towar => towar.JednMiary).HasColumnName("tw_JednMiary").HasMaxLength(10).IsUnicode(false);
        entity.Property(towar => towar.Pkwiu).HasColumnName("tw_PKWiU").HasMaxLength(20).IsUnicode(false);
        entity.Property(towar => towar.Sww).HasColumnName("tw_SWW").HasMaxLength(20).IsUnicode(false);
        entity.Property(towar => towar.IdRabat).HasColumnName("tw_IdRabat");
        entity.Property(towar => towar.IdOpakowanie).HasColumnName("tw_IdOpakowanie");
        entity.Property(towar => towar.PrzezWartosc).HasColumnName("tw_PrzezWartosc");
        entity.Property(towar => towar.IdPodstDostawca).HasColumnName("tw_IdPodstDostawca");
        entity.Property(towar => towar.DostSymbol).HasColumnName("tw_DostSymbol").HasMaxLength(20).IsUnicode(false);
        entity.Property(towar => towar.CzasDostawy).HasColumnName("tw_CzasDostawy");
        entity.Property(towar => towar.UrzNazwa).HasColumnName("tw_UrzNazwa").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Plu).HasColumnName("tw_PLU");
        entity.Property(towar => towar.PodstKodKresk).HasColumnName("tw_PodstKodKresk").HasMaxLength(20).IsUnicode(false);
        entity.Property(towar => towar.IdTypKodu).HasColumnName("tw_IdTypKodu");
        entity.Property(towar => towar.CenaOtwarta).HasColumnName("tw_CenaOtwarta");
        entity.Property(towar => towar.WagaEtykiet).HasColumnName("tw_WagaEtykiet");
        entity.Property(towar => towar.KontrolaTW).HasColumnName("tw_KontrolaTW");
        entity.Property(towar => towar.StanMin).HasColumnName("tw_StanMin").HasColumnType("money");
        entity.Property(towar => towar.JednStanMin).HasColumnName("tw_JednStanMin").HasMaxLength(10).IsUnicode(false);
        entity.Property(towar => towar.DniWaznosc).HasColumnName("tw_DniWaznosc");
        entity.Property(towar => towar.IdGrupa).HasColumnName("tw_IdGrupa");
        entity.Property(towar => towar.Www).HasColumnName("tw_WWW").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.SklepInternet).HasColumnName("tw_SklepInternet");
        entity.Property(towar => towar.Pole1).HasColumnName("tw_Pole1").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole2).HasColumnName("tw_Pole2").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole3).HasColumnName("tw_Pole3").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole4).HasColumnName("tw_Pole4").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole5).HasColumnName("tw_Pole5").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole6).HasColumnName("tw_Pole6").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole7).HasColumnName("tw_Pole7").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Pole8).HasColumnName("tw_Pole8").HasMaxLength(50).IsUnicode(false);
        entity.Property(towar => towar.Uwagi).HasColumnName("tw_Uwagi").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.Logo).HasColumnName("tw_Logo").HasColumnType("binary(50)");
        entity.Property(towar => towar.Usuniety).HasColumnName("tw_Usuniety");
        entity.Property(towar => towar.Objetosc).HasColumnName("tw_Objetosc").HasColumnType("money");
        entity.Property(towar => towar.Masa).HasColumnName("tw_Masa").HasColumnType("money");
        entity.Property(towar => towar.Charakter).HasColumnName("tw_Charakter").HasColumnType("text");
        entity.Property(towar => towar.JednMiaryZak).HasColumnName("tw_JednMiaryZak").HasMaxLength(10).IsUnicode(false);
        entity.Property(towar => towar.JmZakInna).HasColumnName("tw_JMZakInna");
        entity.Property(towar => towar.KodTowaru).HasColumnName("tw_KodTowaru").HasMaxLength(20).IsUnicode(false);
        entity.Property(towar => towar.IdKrajuPochodzenia).HasColumnName("tw_IdKrajuPochodzenia");
        entity.Property(towar => towar.IdUjm).HasColumnName("tw_IdUJM");
        entity.Property(towar => towar.JednMiarySprz).HasColumnName("tw_JednMiarySprz").HasMaxLength(10).IsUnicode(false);
        entity.Property(towar => towar.JmSprzInna).HasColumnName("tw_JMSprzInna");
        entity.Property(towar => towar.SerwisAukcyjny).HasColumnName("tw_SerwisAukcyjny");
        entity.Property(towar => towar.IdProducenta).HasColumnName("tw_IdProducenta");
        entity.Property(towar => towar.SprzedazMobilna).HasColumnName("tw_SprzedazMobilna");
        entity.Property(towar => towar.IsFundPromocji).HasColumnName("tw_IsFundPromocji");
        entity.Property(towar => towar.IdFundPromocji).HasColumnName("tw_IdFundPromocji");
        entity.Property(towar => towar.DomyslnaKategoria).HasColumnName("tw_DomyslnaKategoria");
        entity.Property(towar => towar.Wysokosc).HasColumnName("tw_Wysokosc").HasColumnType("money");
        entity.Property(towar => towar.Szerokosc).HasColumnName("tw_Szerokosc").HasColumnType("money");
        entity.Property(towar => towar.Glebokosc).HasColumnName("tw_Glebokosc").HasColumnType("money");
        entity.Property(towar => towar.StanMaks).HasColumnName("tw_StanMaks").HasColumnType("money");
        entity.Property(towar => towar.Akcyza).HasColumnName("tw_Akcyza");
        entity.Property(towar => towar.AkcyzaZaznacz).HasColumnName("tw_AkcyzaZaznacz");
        entity.Property(towar => towar.AkcyzaKwota).HasColumnName("tw_AkcyzaKwota").HasColumnType("money");
        entity.Property(towar => towar.ObrotMarza).HasColumnName("tw_ObrotMarza");
        entity.Property(towar => towar.OdwrotneObciazenie).HasColumnName("tw_OdwrotneObciazenie");
        entity.Property(towar => towar.ProgKwotowyOO).HasColumnName("tw_ProgKwotowyOO");
        entity.Property(towar => towar.DodawalnyDoZW).HasColumnName("tw_DodawalnyDoZW");
        entity.Property(towar => towar.Isbn).HasColumnName("tw_isbn").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.Bloz7).HasColumnName("tw_bloz_7").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.Bloz12).HasColumnName("tw_bloz_12").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.KodUProducenta).HasColumnName("tw_KodUProducenta").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.Komunikat).HasColumnName("tw_Komunikat").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.KomunikatOd).HasColumnName("tw_KomunikatOd").HasColumnType("datetime");
        entity.Property(towar => towar.KomunikatDokumenty).HasColumnName("tw_KomunikatDokumenty");
        entity.Property(towar => towar.MechanizmPodzielonejPlatnosci).HasColumnName("tw_MechanizmPodzielonejPlatnosci");
        entity.Property(towar => towar.GrupaJpkVat).HasColumnName("tw_GrupaJpkVat");
        entity.Property(towar => towar.OplCukrowaPodlega).HasColumnName("tw_OplCukrowaPodlega");
        entity.Property(towar => towar.OplCukrowaObj).HasColumnName("tw_OplCukrowaObj").HasColumnType("money");
        entity.Property(towar => towar.OplCukrowaZawartoscCukru).HasColumnName("tw_OplCukrowaZawartoscCukru").HasColumnType("money");
        entity.Property(towar => towar.OplCukrowaInneSlodzace).HasColumnName("tw_OplCukrowaInneSlodzace");
        entity.Property(towar => towar.OplCukrowaSok).HasColumnName("tw_OplCukrowaSok");
        entity.Property(towar => towar.OplCukrowaKwota).HasColumnName("tw_OplCukrowaKwota").HasColumnType("money");
        entity.Property(towar => towar.OplCukrowaKofeinaPodlega).HasColumnName("tw_OplCukrowaKofeinaPodlega");
        entity.Property(towar => towar.OplCukrowaKofeinaKwota).HasColumnName("tw_OplCukrowaKofeinaKwota").HasColumnType("money");
        entity.Property(towar => towar.OplCukrowaNapojWeglElektr).HasColumnName("tw_OplCukrowaNapojWeglElektr");
        entity.Property(towar => towar.OplCukrowaKwotaPowyzej).HasColumnName("tw_OplCukrowaKwotaPowyzej").HasColumnType("money");
        entity.Property(towar => towar.MasaNetto).HasColumnName("tw_MasaNetto").HasColumnType("money");
        entity.Property(towar => towar.IdKoduWyrobuAkcyzowego).HasColumnName("tw_IdKoduWyrobuAkcyzowego");
        entity.Property(towar => towar.AkcyzaMarkaWyrobow).HasColumnName("tw_AkcyzaMarkaWyrobow").HasMaxLength(350).IsUnicode(false);
        entity.Property(towar => towar.AkcyzaWielkoscProducenta).HasColumnName("tw_AkcyzaWielkoscProducenta").HasMaxLength(15).IsUnicode(false);
        entity.Property(towar => towar.ZnakiAkcyzy).HasColumnName("tw_ZnakiAkcyzy");
        entity.Property(towar => towar.DataZmianyVatSprzedazy).HasColumnName("tw_DataZmianyVatSprzedazy").HasColumnType("datetime");
        entity.Property(towar => towar.WegielPodlegaOswiadczeniu).HasColumnName("tw_WegielPodlegaOswiadczeniu");
        entity.Property(towar => towar.WegielOpisPochodzenia).HasColumnName("tw_WegielOpisPochodzenia").HasMaxLength(255).IsUnicode(false);
        entity.Property(towar => towar.PodlegaOplacieNaFunduszOchronyRolnictwa).HasColumnName("tw_PodlegaOplacieNaFunduszOchronyRolnictwa");
        entity.Property(towar => towar.ObjetySysKaucyjnym).HasColumnName("tw_ObjetySysKaucyjnym");
        entity.Property(towar => towar.SKRodzajOpakowania).HasColumnName("tw_SKRodzajOpakowania");
        entity.Property(towar => towar.SKOpakowanieZwracane).HasColumnName("tw_SKOpakowanieZwracane");
        entity.Property(towar => towar.SKIdOpakowania).HasColumnName("tw_SKIdOpakowania");
    }
}
