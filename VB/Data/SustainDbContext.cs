using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Sustain.Models;

namespace Sustain.Data;

public partial class SustainDbContext : DbContext
{
    public SustainDbContext()
    {
    }

    public SustainDbContext(DbContextOptions<SustainDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EmissionRecord> EmissionRecords { get; set; }

    public virtual DbSet<EmissionSource> EmissionSources { get; set; }

    public virtual DbSet<Factory> Factories { get; set; }

    public virtual DbSet<Goal> Goals { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmissionRecord>(entity =>
        {
            entity.HasKey(e => e.EmissionRecordId).HasName("PK__Emission__16633759AA0A395B");

            entity.ToTable("Emission_Record");

            entity.Property(e => e.EmissionRecordId).HasColumnName("EmissionRecord_ID");
            entity.Property(e => e.EmissionCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Emission_CreatedAt");
            entity.Property(e => e.EmissionMonth).HasColumnName("Emission_Month");
            entity.Property(e => e.EmissionQuantity)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("Emission_Quantity");
            entity.Property(e => e.EmissionSourceId).HasColumnName("EmissionSource_ID");
            entity.Property(e => e.EmissionUpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Emission_UpdatedAt");
            entity.Property(e => e.EmissionYear).HasColumnName("Emission_Year");
            entity.Property(e => e.FactoryId).HasColumnName("Factory_ID");
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.EmissionSource).WithMany(p => p.EmissionRecords)
                .HasForeignKey(d => d.EmissionSourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Emission___Emiss__59063A47");

            entity.HasOne(d => d.Factory).WithMany(p => p.EmissionRecords)
                .HasForeignKey(d => d.FactoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Emission___Facto__59FA5E80");

            entity.HasOne(d => d.Organization).WithMany(p => p.EmissionRecords)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Emission___Organ__5AEE82B9");

            entity.HasOne(d => d.User).WithMany(p => p.EmissionRecords)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Emission___User___5BE2A6F2");
        });

        modelBuilder.Entity<EmissionSource>(entity =>
        {
            entity.HasKey(e => e.EmissionSourceId).HasName("PK__Emission__E917A97ED9E68A41");

            entity.ToTable("Emission_Source");

            entity.Property(e => e.EmissionSourceId).HasColumnName("EmissionSource_ID");

            // ADD THIS LINE - Map OrganizationId to the database column
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");

            entity.Property(e => e.EmissionSourceDescription)
                .HasMaxLength(255)
                .HasColumnName("EmissionSource_Description");
            entity.Property(e => e.EmissionSourceEmissionFactor)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("EmissionSource_EmissionFactor");
            entity.Property(e => e.EmissionSourceFormula)
                .HasMaxLength(255)
                .HasColumnName("EmissionSource_Formula");
            entity.Property(e => e.EmissionSourceIsActive)
                .HasDefaultValue(true)
                .HasColumnName("EmissionSource_IsActive");
            entity.Property(e => e.EmissionSourceIsRequested)
                .HasDefaultValue(false)
                .HasColumnName("EmissionSource_IsRequested");
            entity.Property(e => e.EmissionSourceName)
                .HasMaxLength(150)
                .HasColumnName("EmissionSource_Name");
            entity.Property(e => e.EmissionSourcePeriod)
                .HasMaxLength(50)
                .HasColumnName("EmissionSource_Period");
            entity.Property(e => e.EmissionSourceRequestStatus)
                .HasMaxLength(50)
                .HasColumnName("EmissionSource_RequestStatus");
            entity.Property(e => e.EmissionSourceRequestedAt)
                .HasColumnType("datetime")
                .HasColumnName("EmissionSource_RequestedAt");
            entity.Property(e => e.EmissionSourceScope)
                .HasMaxLength(50)
                .HasColumnName("EmissionSource_Scope");
            entity.Property(e => e.EmissionSourceUnit)
                .HasMaxLength(50)
                .HasColumnName("EmissionSource_Unit");

            // ADD THIS RELATIONSHIP CONFIGURATION at the end (before the closing bracket)
            entity.HasOne(d => d.Organization).WithMany(p => p.EmissionSources)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Emission_Source__Organization");
        });

        modelBuilder.Entity<Factory>(entity =>
        {
            entity.HasKey(e => e.FactoryId).HasName("PK__Factory__7957ECD072A9690C");

            entity.ToTable("Factory");

            entity.HasIndex(e => e.FactoryCode, "UQ__Factory__4122ADDB56C413AB").IsUnique();

            entity.Property(e => e.FactoryId).HasColumnName("Factory_ID");
            entity.Property(e => e.FactoryCode)
                .HasMaxLength(50)
                .HasColumnName("Factory_Code");
            entity.Property(e => e.FactoryLocation)
                .HasMaxLength(150)
                .HasColumnName("Factory_Location");
            entity.Property(e => e.FactoryName)
                .HasMaxLength(150)
                .HasColumnName("Factory_Name");
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");

            entity.HasOne(d => d.Organization).WithMany(p => p.Factories)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Factory__Organiz__5CD6CB2B");
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(e => e.GoalId).HasName("PK__Goal__09AD12A2936D84B2");

            entity.ToTable("Goal");

            entity.Property(e => e.GoalId).HasColumnName("Goal_ID");
            entity.Property(e => e.EmissionSourceId).HasColumnName("EmissionSource_ID");
            entity.Property(e => e.GoalDescription)
                .HasMaxLength(255)
                .HasColumnName("Goal_Description");
            entity.Property(e => e.GoalGoalEndDate).HasColumnName("Goal_GoalEndDate");
            entity.Property(e => e.GoalGoalPeriod)
                .HasMaxLength(50)
                .HasColumnName("Goal_GoalPeriod");
            entity.Property(e => e.GoalGoalStartDate).HasColumnName("Goal_GoalStartDate");
            entity.Property(e => e.GoalGoalValue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Goal_GoalValue");
            entity.Property(e => e.GoalStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("Goal_Status");
            entity.Property(e => e.GoalTitle)
                .HasMaxLength(150)
                .HasColumnName("Goal_Title");
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.EmissionSource).WithMany(p => p.Goals)
                .HasForeignKey(d => d.EmissionSourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Goal__EmissionSo__5DCAEF64");

            entity.HasOne(d => d.Organization).WithMany(p => p.Goals)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Goal__Organizati__5EBF139D");

            entity.HasOne(d => d.User).WithMany(p => p.Goals)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Goal__User_ID__5FB337D6");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.InvitationId).HasName("PK__Invitati__41C0DCC53688D569");

            entity.ToTable("Invitation");

            entity.Property(e => e.InvitationId).HasColumnName("Invitation_ID");
            entity.Property(e => e.FactoryId).HasColumnName("Factory_ID");
            entity.Property(e => e.InvitationAcceptedAt)
                .HasColumnType("datetime")
                .HasColumnName("Invitation_AcceptedAt");
            entity.Property(e => e.InvitationExpiration)
                .HasColumnType("datetime")
                .HasColumnName("Invitation_Expiration");
            entity.Property(e => e.InvitationSentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Invitation_SentAt");
            entity.Property(e => e.InvitationStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .HasColumnName("Invitation_Status");
            entity.Property(e => e.InvitationToken)
                .HasMaxLength(200)
                .HasColumnName("Invitation_Token");
            entity.Property(e => e.InvitedEmail)
                .HasMaxLength(150)
                .HasColumnName("Invited_Email");
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Factory).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.FactoryId)
                .HasConstraintName("FK__Invitatio__Facto__7C4F7684");

            entity.HasOne(d => d.Organization).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invitatio__Organ__7A672E12");

            entity.HasOne(d => d.Role).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invitatio__Role___7B5B524B");

            entity.HasOne(d => d.User).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Invitatio__User___7D439ABD");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoice__0DE6049419BADD32");

            entity.ToTable("Invoice");

            entity.Property(e => e.InvoiceId).HasColumnName("Invoice_ID");
            entity.Property(e => e.InvoiceDate).HasColumnName("Invoice_Date");
            entity.Property(e => e.InvoiceStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .HasColumnName("Invoice_Status");
            entity.Property(e => e.InvoiceTime).HasColumnName("Invoice_Time");
            entity.Property(e => e.InvoiceTotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("Invoice_Total_Amount");
            entity.Property(e => e.SubscriptionId).HasColumnName("Subscription_ID");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invoice__Subscri__6477ECF3");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.OrganizationId).HasName("PK__Organiza__A6FA2506C163D7B9");

            entity.ToTable("Organization");

            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");
            entity.Property(e => e.OrganizationCity)
                .HasMaxLength(100)
                .HasColumnName("Organization_City");
            entity.Property(e => e.OrganizationIndustry)
                .HasMaxLength(100)
                .HasColumnName("Organization_Industry");
            entity.Property(e => e.OrganizationName)
                .HasMaxLength(150)
                .HasColumnName("Organization_Name");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__DA6C7FE157E66D5B");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).HasColumnName("Payment_ID");
            entity.Property(e => e.InvoiceId).HasColumnName("Invoice_ID");
            entity.Property(e => e.PaymentAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("Payment_Amount");
            entity.Property(e => e.PaymentDate).HasColumnName("Payment_Date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("Payment_Method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Completed")
                .HasColumnName("Payment_Status");
            entity.Property(e => e.PaymentTime).HasColumnName("Payment_Time");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Invoice__656C112C");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__D80AB49B04D1A887");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasColumnName("Role_Name");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__518059B168F5A54F");

            entity.ToTable("Subscription");

            entity.Property(e => e.SubscriptionId).HasColumnName("Subscription_ID");
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");
            entity.Property(e => e.SubscriptionEndDate).HasColumnName("Subscription_EndDate");
            entity.Property(e => e.SubscriptionPlanId).HasColumnName("SubscriptionPlan_ID");
            entity.Property(e => e.SubscriptionStartDate).HasColumnName("Subscription_StartDate");
            entity.Property(e => e.SubscriptionStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("Subscription_Status");

            entity.HasOne(d => d.Organization).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subscript__Organ__66603565");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subscript__Subsc__6754599E");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.SubscriptionPlanId).HasName("PK__Subscrip__F18A6C6BB22AF5E5");

            entity.ToTable("SubscriptionPlan");

            entity.Property(e => e.SubscriptionPlanId).HasColumnName("SubscriptionPlan_ID");
            entity.Property(e => e.SubscriptionPlanDescription)
                .HasMaxLength(255)
                .HasColumnName("SubscriptionPlan_Description");
            entity.Property(e => e.SubscriptionPlanDuration).HasColumnName("SubscriptionPlan_Duration");
            entity.Property(e => e.SubscriptionPlanFactoryMax).HasColumnName("SubscriptionPlan_FactoryMax");
            entity.Property(e => e.SubscriptionPlanName)
                .HasMaxLength(100)
                .HasColumnName("SubscriptionPlan_Name");
            entity.Property(e => e.SubscriptionPlanPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SubscriptionPlan_Price");
            entity.Property(e => e.SubscriptionPlanType)
                .HasMaxLength(10)
                .HasColumnName("SubscriptionPlan_Type");
            entity.Property(e => e.SubscriptionPlanUserMax).HasColumnName("SubscriptionPlan_UserMax");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__206D91908C210E91");

            entity.ToTable("User");

            entity.HasIndex(e => e.UserEmail, "UQ__User__4C70A05C916667E8").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.OrganizationId).HasColumnName("Organization_ID");
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(150)
                .HasColumnName("User_Email");
            entity.Property(e => e.UserFname)
                .HasMaxLength(100)
                .HasColumnName("User_FName");
            entity.Property(e => e.UserLname)
                .HasMaxLength(100)
                .HasColumnName("User_LName");
            entity.Property(e => e.UserPassword)
                .HasMaxLength(255)
                .HasColumnName("User_Password");
            entity.Property(e => e.UserPhone)
                .HasMaxLength(20)
                .HasColumnName("User_Phone");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("User_Status");

            entity.HasOne(d => d.Organization).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__Organizati__68487DD7");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__Role_ID__693CA210");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
