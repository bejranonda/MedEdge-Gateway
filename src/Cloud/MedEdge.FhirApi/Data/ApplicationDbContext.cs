using MedEdge.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedEdge.FhirApi.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<FhirPatientEntity> Patients { get; set; } = default!;
    public DbSet<FhirDeviceEntity> Devices { get; set; } = default!;
    public DbSet<FhirObservationEntity> Observations { get; set; } = default!;

    // Treatment Center Entities
    public DbSet<TreatmentAreaEntity> TreatmentAreas { get; set; } = default!;
    public DbSet<StationEntity> Stations { get; set; } = default!;
    public DbSet<StationConfigurationEntity> StationConfigurations { get; set; } = default!;
    public DbSet<SupplyEntity> Supplies { get; set; } = default!;
    public DbSet<StationSupplyEntity> StationSupplies { get; set; } = default!;
    public DbSet<SupplyLotEntity> SupplyLots { get; set; } = default!;
    public DbSet<TreatmentSessionEntity> TreatmentSessions { get; set; } = default!;
    public DbSet<TreatmentPhaseEntity> TreatmentPhases { get; set; } = default!;
    public DbSet<TreatmentObservationEntity> TreatmentObservations { get; set; } = default!;
    public DbSet<TreatmentOutcomeEntity> TreatmentOutcomes { get; set; } = default!;
    public DbSet<DeviceGroupEntity> DeviceGroups { get; set; } = default!;
    public DbSet<DeviceCoordinationCommand> CoordinationCommands { get; set; } = default!;
    public DbSet<TreatmentMetricsEntity> TreatmentMetrics { get; set; } = default!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Patient entity
        modelBuilder.Entity<FhirPatientEntity>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<FhirPatientEntity>()
            .HasMany(p => p.AssignedDevices)
            .WithOne(d => d.AssignedPatient)
            .HasForeignKey(d => d.AssignedPatientId);

        modelBuilder.Entity<FhirPatientEntity>()
            .HasMany(p => p.Observations)
            .WithOne(o => o.Patient)
            .HasForeignKey(o => o.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Device entity
        modelBuilder.Entity<FhirDeviceEntity>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<FhirDeviceEntity>()
            .HasMany(d => d.Observations)
            .WithOne(o => o.Device)
            .HasForeignKey(o => o.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Observation entity
        modelBuilder.Entity<FhirObservationEntity>()
            .HasKey(o => o.Id);

        modelBuilder.Entity<FhirObservationEntity>()
            .HasIndex(o => o.PatientId);

        modelBuilder.Entity<FhirObservationEntity>()
            .HasIndex(o => o.DeviceId);

        modelBuilder.Entity<FhirObservationEntity>()
            .HasIndex(o => o.Code);

        modelBuilder.Entity<FhirObservationEntity>()
            .HasIndex(o => o.ObservationTime);

        // Configure TreatmentArea entity
        modelBuilder.Entity<TreatmentAreaEntity>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<TreatmentAreaEntity>()
            .HasIndex(a => a.ParentAreaId);

        modelBuilder.Entity<TreatmentAreaEntity>()
            .HasMany(a => a.SubAreas)
            .WithOne(a => a.ParentArea)
            .HasForeignKey(a => a.ParentAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TreatmentAreaEntity>()
            .HasMany(a => a.Stations)
            .WithOne(s => s.Area)
            .HasForeignKey(s => s.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Station entity
        modelBuilder.Entity<StationEntity>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<StationEntity>()
            .HasIndex(s => s.AreaId);

        modelBuilder.Entity<StationEntity>()
            .HasIndex(s => s.Status);

        modelBuilder.Entity<StationEntity>()
            .HasIndex(s => new { s.AreaId, s.Status }); // Composite for querying available stations by area

        modelBuilder.Entity<StationEntity>()
            .HasOne(s => s.Configuration)
            .WithOne(c => c.Station)
            .HasForeignKey<StationConfigurationEntity>(c => c.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StationEntity>()
            .HasMany(s => s.Devices)
            .WithOne(d => d.Station)
            .HasForeignKey(d => d.StationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure StationConfiguration entity
        modelBuilder.Entity<StationConfigurationEntity>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<StationConfigurationEntity>()
            .HasIndex(c => c.StationId)
            .IsUnique();

        // Configure Supply entity
        modelBuilder.Entity<SupplyEntity>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<SupplyEntity>()
            .HasIndex(s => s.Sku)
            .IsUnique();

        modelBuilder.Entity<SupplyEntity>()
            .HasIndex(s => s.Category);

        modelBuilder.Entity<SupplyEntity>()
            .HasMany(s => s.StationSupplies)
            .WithOne(ss => ss.Supply)
            .HasForeignKey(ss => ss.SupplyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure StationSupply entity
        modelBuilder.Entity<StationSupplyEntity>()
            .HasKey(ss => ss.Id);

        modelBuilder.Entity<StationSupplyEntity>()
            .HasIndex(ss => new { ss.StationId, ss.SupplyId })
            .IsUnique(); // One supply entry per station per supply type

        modelBuilder.Entity<StationSupplyEntity>()
            .HasMany(ss => ss.Lots)
            .WithOne(l => l.StationSupply)
            .HasForeignKey(l => l.StationSupplyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure SupplyLot entity
        modelBuilder.Entity<SupplyLotEntity>()
            .HasKey(l => l.Id);

        modelBuilder.Entity<SupplyLotEntity>()
            .HasIndex(l => l.ExpirationDate);

        modelBuilder.Entity<SupplyLotEntity>()
            .HasIndex(l => new { l.StationSupplyId, l.IsExpired });

        // Configure TreatmentSession entity
        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasKey(ts => ts.Id);

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasIndex(ts => ts.PatientId);

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasIndex(ts => ts.StationId);

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasIndex(ts => ts.Status);

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasIndex(ts => ts.ScheduledStart);

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasIndex(ts => new { ts.StationId, ts.Status }); // Composite for active treatments by station

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasOne(ts => ts.Patient)
            .WithMany()
            .HasForeignKey(ts => ts.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TreatmentSessionEntity>()
            .HasOne(ts => ts.Station)
            .WithMany()
            .HasForeignKey(ts => ts.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure TreatmentPhase entity
        modelBuilder.Entity<TreatmentPhaseEntity>()
            .HasKey(tp => tp.Id);

        modelBuilder.Entity<TreatmentPhaseEntity>()
            .HasIndex(tp => tp.TreatmentSessionId);

        modelBuilder.Entity<TreatmentPhaseEntity>()
            .HasIndex(tp => tp.Status);

        modelBuilder.Entity<TreatmentPhaseEntity>()
            .HasOne(tp => tp.TreatmentSession)
            .WithMany(ts => ts.Phases)
            .HasForeignKey(tp => tp.TreatmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure TreatmentObservation entity
        modelBuilder.Entity<TreatmentObservationEntity>()
            .HasKey(to => to.Id);

        modelBuilder.Entity<TreatmentObservationEntity>()
            .HasIndex(to => to.TreatmentSessionId);

        modelBuilder.Entity<TreatmentObservationEntity>()
            .HasIndex(to => to.ObservationType);

        modelBuilder.Entity<TreatmentObservationEntity>()
            .HasIndex(to => to.RecordedAt);

        modelBuilder.Entity<TreatmentObservationEntity>()
            .HasOne(to => to.TreatmentSession)
            .WithMany(ts => ts.Observations)
            .HasForeignKey(to => to.TreatmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure TreatmentOutcome entity
        modelBuilder.Entity<TreatmentOutcomeEntity>()
            .HasKey(to => to.Id);

        modelBuilder.Entity<TreatmentOutcomeEntity>()
            .HasIndex(to => to.TreatmentSessionId);

        modelBuilder.Entity<TreatmentOutcomeEntity>()
            .HasIndex(to => to.OutcomeType);

        modelBuilder.Entity<TreatmentOutcomeEntity>()
            .HasIndex(to => to.WasAdverseEvent);

        modelBuilder.Entity<TreatmentOutcomeEntity>()
            .HasOne(to => to.TreatmentSession)
            .WithMany(ts => ts.Outcomes)
            .HasForeignKey(to => to.TreatmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure DeviceGroup entity
        modelBuilder.Entity<DeviceGroupEntity>()
            .HasKey(dg => dg.Id);

        modelBuilder.Entity<DeviceGroupEntity>()
            .HasIndex(dg => dg.StationId);

        modelBuilder.Entity<DeviceGroupEntity>()
            .HasIndex(dg => new { dg.StationId, dg.IsActive });

        modelBuilder.Entity<DeviceGroupEntity>()
            .HasOne(dg => dg.Station)
            .WithMany()
            .HasForeignKey(dg => dg.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure DeviceCoordinationCommand entity
        modelBuilder.Entity<DeviceCoordinationCommand>()
            .HasKey(dc => dc.Id);

        modelBuilder.Entity<DeviceCoordinationCommand>()
            .HasIndex(dc => dc.StationId);

        modelBuilder.Entity<DeviceCoordinationCommand>()
            .HasIndex(dc => dc.Status);

        modelBuilder.Entity<DeviceCoordinationCommand>()
            .HasIndex(dc => dc.ScheduledExecutionTime);

        modelBuilder.Entity<DeviceCoordinationCommand>()
            .HasOne(dc => dc.Station)
            .WithMany()
            .HasForeignKey(dc => dc.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure TreatmentMetrics entity
        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasKey(tm => tm.Id);

        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasIndex(tm => tm.MetricDate);

        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasIndex(tm => tm.StationId);

        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasIndex(tm => tm.AreaId);

        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasIndex(tm => new { tm.MetricDate, tm.StationId });

        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasOne(tm => tm.Station)
            .WithMany()
            .HasForeignKey(tm => tm.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TreatmentMetricsEntity>()
            .HasOne(tm => tm.Area)
            .WithMany()
            .HasForeignKey(tm => tm.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed initial data
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        // Seed Patients
        var patients = new[]
        {
            new FhirPatientEntity
            {
                Id = "P001",
                Mrn = "P001",
                GivenName = "John",
                FamilyName = "Doe",
                BirthDate = new DateTime(1965, 3, 15),
                Gender = "male",
                CreatedAt = now,
                UpdatedAt = now
            },
            new FhirPatientEntity
            {
                Id = "P002",
                Mrn = "P002",
                GivenName = "Jane",
                FamilyName = "Smith",
                BirthDate = new DateTime(1972, 8, 22),
                Gender = "female",
                CreatedAt = now,
                UpdatedAt = now
            },
            new FhirPatientEntity
            {
                Id = "P003",
                Mrn = "P003",
                GivenName = "Robert",
                FamilyName = "Johnson",
                BirthDate = new DateTime(1958, 11, 30),
                Gender = "male",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        modelBuilder.Entity<FhirPatientEntity>().HasData(patients);

        // Seed Devices
        var devices = new[]
        {
            new FhirDeviceEntity
            {
                Id = "Device-001",
                DeviceId = "Device-001",
                Manufacturer = "Medical Device",
                Model = "Dialog+",
                SerialNumber = "DG001",
                Status = "active",
                AssignedPatientId = "P001",
                CreatedAt = now,
                UpdatedAt = now
            },
            new FhirDeviceEntity
            {
                Id = "Device-002",
                DeviceId = "Device-002",
                Manufacturer = "Medical Device",
                Model = "Dialog iQ",
                SerialNumber = "DQ002",
                Status = "active",
                AssignedPatientId = "P002",
                CreatedAt = now,
                UpdatedAt = now
            },
            new FhirDeviceEntity
            {
                Id = "Device-003",
                DeviceId = "Device-003",
                Manufacturer = "Medical Device",
                Model = "Dialog+",
                SerialNumber = "DG003",
                Status = "active",
                AssignedPatientId = "P003",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        modelBuilder.Entity<FhirDeviceEntity>().HasData(devices);

        // Seed Treatment Areas (6 zones for large scale)
        var treatmentAreas = new[]
        {
            new TreatmentAreaEntity
            {
                Id = "ZONE-A",
                Name = "Zone A",
                AreaType = "dialysis",
                Capacity = 10,
                DisplayOrder = 1,
                Description = "Primary dialysis treatment area",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TreatmentAreaEntity
            {
                Id = "ZONE-B",
                Name = "Zone B",
                AreaType = "dialysis",
                Capacity = 10,
                DisplayOrder = 2,
                Description = "Secondary dialysis treatment area",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TreatmentAreaEntity
            {
                Id = "ZONE-C",
                Name = "Zone C",
                AreaType = "dialysis",
                Capacity = 10,
                DisplayOrder = 3,
                Description = "Third dialysis treatment area",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TreatmentAreaEntity
            {
                Id = "ZONE-D",
                Name = "Zone D",
                AreaType = "dialysis",
                Capacity = 8,
                DisplayOrder = 4,
                Description = "Fourth dialysis treatment area",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TreatmentAreaEntity
            {
                Id = "ZONE-E",
                Name = "Zone E",
                AreaType = "icu",
                Capacity = 6,
                DisplayOrder = 5,
                Description = "Intensive care unit with dialysis capability",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TreatmentAreaEntity
            {
                Id = "ZONE-F",
                Name = "Zone F",
                AreaType = "general",
                Capacity = 8,
                DisplayOrder = 6,
                Description = "General treatment area",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        modelBuilder.Entity<TreatmentAreaEntity>().HasData(treatmentAreas);

        // Seed Station Configurations first (required by Stations)
        var stationConfigurations = new List<StationConfigurationEntity>();
        var stations = new List<StationEntity>();

        // Create stations for each zone (52 total stations)
        int stationIndex = 1;
        foreach (var area in treatmentAreas)
        {
            for (int i = 1; i <= area.Capacity; i++)
            {
                var stationId = $"STATION-{area.Id.Replace("ZONE", "")}-{i:D2}";
                var configId = $"CONFIG-{stationId}";

                stationConfigurations.Add(new StationConfigurationEntity
                {
                    Id = configId,
                    StationId = stationId,
                    HasWaterSupply = area.AreaType == "dialysis",
                    HasPowerBackup = true,
                    HasOxygenSupply = area.AreaType == "icu",
                    HasVacuumSupply = area.AreaType == "dialysis",
                    MaxDeviceSlots = 5,
                    DeviceSlots = new Dictionary<string, string>(),
                    TreatmentTypes = area.AreaType == "dialysis" ? "hemodialysis,peritoneal" : "infusion,general",
                    CreatedAt = now,
                    UpdatedAt = now
                });

                stations.Add(new StationEntity
                {
                    Id = stationId,
                    StationNumber = $"{area.Name.Replace("Zone ", "")}-{i:D2}",
                    Status = "available",
                    AreaId = area.Id,
                    DisplayOrder = i,
                    PhysicalLocation = $"Room {area.Id.Replace("ZONE", "")}{i:D2}",
                    CreatedAt = now,
                    UpdatedAt = now
                });

                stationIndex++;
            }
        }

        modelBuilder.Entity<StationConfigurationEntity>().HasData(stationConfigurations);
        modelBuilder.Entity<StationEntity>().HasData(stations);

        // Seed Supply Catalog
        var supplies = new[]
        {
            new SupplyEntity
            {
                Id = "SUPPLY-001",
                Name = "Dialyzer",
                Category = "consumable",
                Sku = "DIAL-001",
                Description = "Standard dialysis filter",
                UnitOfMeasure = "each",
                ReorderLevel = 10,
                ReorderQuantity = 50,
                ShelfLifeDays = 730,
                StorageLocation = "Supply Room A",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-002",
                Name = "Bloodline Set",
                Category = "consumable",
                Sku = "BLS-001",
                Description = "Arterial and venous bloodline set",
                UnitOfMeasure = "each",
                ReorderLevel = 20,
                ReorderQuantity = 100,
                ShelfLifeDays = 730,
                StorageLocation = "Supply Room A",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-003",
                Name = "Saline Solution 0.9%",
                Category = "fluid",
                Sku = "SAL-1000",
                Description = "1000ml saline solution",
                UnitOfMeasure = "ml",
                ReorderLevel = 50,
                ReorderQuantity = 200,
                ShelfLifeDays = 1095,
                StorageLocation = "Supply Room B",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-004",
                Name = "Heparin Flush",
                Category = "medication",
                Sku = "HEP-100",
                Description = "100 unit/ml heparin flush solution",
                UnitOfMeasure = "ml",
                ReorderLevel = 30,
                ReorderQuantity = 100,
                ShelfLifeDays = 730,
                StorageLocation = "Medication Cabinet",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-005",
                Name = "Infusion Pump Cassette",
                Category = "consumable",
                Sku = "INF-CAS",
                Description = "Disposable infusion pump cassette",
                UnitOfMeasure = "each",
                ReorderLevel = 15,
                ReorderQuantity = 75,
                ShelfLifeDays = 730,
                StorageLocation = "Supply Room A",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-006",
                Name = "Dialysate Concentrate",
                Category = "fluid",
                Sku = "DIAL-CONC",
                Description = "Acidic dialysate concentrate",
                UnitOfMeasure = "l",
                ReorderLevel = 100,
                ReorderQuantity = 500,
                ShelfLifeDays = 365,
                StorageLocation = "Supply Room C",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-007",
                Name = "Sodium Bicarbonate",
                Category = "fluid",
                Sku = "BICARB",
                Description = "Sodium bicarbonate solution for dialysis",
                UnitOfMeasure = "l",
                ReorderLevel = 80,
                ReorderQuantity = 400,
                ShelfLifeDays = 365,
                StorageLocation = "Supply Room C",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SupplyEntity
            {
                Id = "SUPPLY-008",
                Name = "Sterile Gauze Pads",
                Category = "consumable",
                Sku = "GAUZE-4x4",
                Description = "4x4 sterile gauze pads",
                UnitOfMeasure = "each",
                ReorderLevel = 100,
                ReorderQuantity = 500,
                ShelfLifeDays = 1825,
                StorageLocation = "Supply Room A",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        modelBuilder.Entity<SupplyEntity>().HasData(supplies);
    }
}
