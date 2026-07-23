using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Infrastructure.Persistence.Entities;

namespace SubiektMobile.Infrastructure.Persistence.Configurations;

public sealed class DokDokumentConfiguration : IEntityTypeConfiguration<DokDokument>
{
    public void Configure(EntityTypeBuilder<DokDokument> entity)
    {
        entity.ToTable("dok__Dokument");
        entity.HasKey(x => x.Id).HasName("PK_dok__Dokument");
        entity.Property(x => x.Id).HasColumnName("dok_Id");
        entity.Property(x => x.Type).HasColumnName("dok_Typ");
        entity.Property(x => x.FullNumber).HasColumnName("dok_NrPelny").HasMaxLength(30).IsUnicode(false);
        entity.Property(x => x.IssuedAt).HasColumnName("dok_DataWyst");
        entity.Property(x => x.FulfilmentDueAt).HasColumnName("dok_TerminRealizacji");
        entity.Property(x => x.RecipientId).HasColumnName("dok_OdbiorcaId");
        entity.Property(x => x.Notes).HasColumnName("dok_Uwagi").HasMaxLength(500).IsUnicode(false);
        entity.Property(x => x.Status).HasColumnName("dok_Status");
        entity.HasIndex(x => new { x.Type, x.Status }).HasDatabaseName("IX_dok__Dokument_Typ_Status");
    }
}

public sealed class DokPozycjaConfiguration : IEntityTypeConfiguration<DokPozycja>
{
    public void Configure(EntityTypeBuilder<DokPozycja> entity)
    {
        entity.ToTable("dok_Pozycja");
        entity.HasKey(x => x.Id).HasName("PK_dok_Pozycja");
        entity.Property(x => x.Id).HasColumnName("ob_Id");
        entity.Property(x => x.CommercialDocumentId).HasColumnName("ob_DokHanId");
        entity.Property(x => x.ProductId).HasColumnName("ob_TowId");
        entity.Property(x => x.ProductKind).HasColumnName("ob_TowRodzaj");
        entity.Property(x => x.Description).HasColumnName("ob_Opis").HasMaxLength(255).IsUnicode(false);
        entity.Property(x => x.LineNumber).HasColumnName("ob_DokHanLp");
        entity.Property(x => x.Quantity).HasColumnName("ob_Ilosc").HasColumnType("money");
        entity.Property(x => x.Unit).HasColumnName("ob_Jm").HasMaxLength(10).IsUnicode(false);
        entity.HasIndex(x => x.CommercialDocumentId).HasDatabaseName("IX_dok_Pozycja_DokHanId");
    }
}
