using Hl7.Fhir.Model;
using MedEdge.Core.Domain.Entities;
using MedEdge.Core.DTOs;

namespace MedEdge.FhirApi.Services;

public interface IFhirMappingService
{
    Patient MapPatientEntityToFhir(FhirPatientEntity entity);
    PatientDto MapFhirPatientToDto(Patient fhirPatient, FhirPatientEntity? entity);
    Device MapDeviceEntityToFhir(FhirDeviceEntity entity);
    DeviceDto MapFhirDeviceToDto(Device fhirDevice, FhirDeviceEntity? entity);
    Observation MapObservationEntityToFhir(FhirObservationEntity entity);
    ObservationDto MapFhirObservationToDto(Observation fhirObs, FhirObservationEntity? entity);
}

public class FhirMappingService : IFhirMappingService
{
    public Patient MapPatientEntityToFhir(FhirPatientEntity entity)
    {
        return new Patient
        {
            Id = entity.Id,
            Identifier = new List<Identifier>
            {
                new()
                {
                    System = "http://example.org/mrn",
                    Value = entity.Mrn
                }
            },
            Name = new List<HumanName>
            {
                new()
                {
                    Given = new[] { entity.GivenName },
                    Family = entity.FamilyName,
                    Use = HumanName.NameUse.Official
                }
            },
            BirthDate = entity.BirthDate.ToString("yyyy-MM-dd"),
            Gender = entity.Gender switch
            {
                "male" => AdministrativeGender.Male,
                "female" => AdministrativeGender.Female,
                "other" => AdministrativeGender.Other,
                _ => AdministrativeGender.Unknown
            }
        };
    }

    public PatientDto MapFhirPatientToDto(Patient fhirPatient, FhirPatientEntity? entity)
    {
        return new PatientDto(
            fhirPatient.Id ?? "unknown",
            entity?.Mrn ?? "unknown",
            entity?.GivenName ?? "unknown",
            entity?.FamilyName ?? "unknown",
            entity?.BirthDate ?? DateTime.MinValue,
            entity?.Gender ?? "unknown"
        );
    }

    public Device MapDeviceEntityToFhir(FhirDeviceEntity entity)
    {
        return new Device
        {
            Id = entity.Id,
            Identifier = new List<Identifier>
            {
                new()
                {
                    System = "http://bbraun.com/device-id",
                    Value = entity.DeviceId
                },
                new()
                {
                    System = "http://bbraun.com/serial",
                    Value = entity.SerialNumber
                }
            },
            Manufacturer = entity.Manufacturer,
            ModelNumber = entity.Model,
            Status = entity.Status switch
            {
                "active" => Device.FHIRDeviceStatus.Active,
                "inactive" => Device.FHIRDeviceStatus.Inactive,
                _ => Device.FHIRDeviceStatus.UnknownStatus
            },
            Type = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new()
                    {
                        System = "http://snomed.info/sct",
                        Code = "108662001",
                        Display = "Hemodialysis machine"
                    }
                }
            }
        };
    }

    public DeviceDto MapFhirDeviceToDto(Device fhirDevice, FhirDeviceEntity? entity)
    {
        return new DeviceDto(
            fhirDevice.Id ?? "unknown",
            entity?.DeviceId ?? "unknown",
            entity?.Manufacturer ?? "unknown",
            entity?.Model ?? "unknown",
            entity?.SerialNumber ?? "unknown",
            entity?.Status ?? "unknown"
        );
    }

    public Observation MapObservationEntityToFhir(FhirObservationEntity entity)
    {
        return new Observation
        {
            Id = entity.Id,
            Status = entity.Status switch
            {
                "preliminary" => ObservationStatus.Preliminary,
                "final" => ObservationStatus.Final,
                "amended" => ObservationStatus.Amended,
                "cancelled" => ObservationStatus.Cancelled,
                _ => ObservationStatus.Final
            },
            Code = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new()
                    {
                        System = "http://loinc.org",
                        Code = entity.Code,
                        Display = entity.CodeDisplay
                    }
                }
            },
            Subject = new ResourceReference($"Patient/{entity.PatientId}"),
            Device = new ResourceReference($"Device/{entity.DeviceId}"),
            Effective = new FhirDateTime(entity.ObservationTime),
            Value = new Quantity
            {
                Value = (decimal)entity.Value,
                Unit = entity.Unit,
                System = "http://unitsofmeasure.org",
                Code = GetUcumCode(entity.Unit)
            }
        };
    }

    public ObservationDto MapFhirObservationToDto(Observation fhirObs, FhirObservationEntity? entity)
    {
        var value = entity?.Value ?? 0;
        var unit = entity?.Unit ?? "unknown";
        var code = entity?.Code ?? "unknown";
        var codeDisplay = entity?.CodeDisplay ?? "unknown";

        return new ObservationDto(
            fhirObs.Id ?? "unknown",
            entity?.PatientId ?? "unknown",
            entity?.DeviceId ?? "unknown",
            code,
            codeDisplay,
            value,
            unit,
            entity?.Status ?? "final",
            entity?.ObservationTime ?? DateTime.UtcNow
        );
    }

    private static string GetUcumCode(string unit)
    {
        return unit switch
        {
            "mL/min" => "mL/min",
            "mmHg" => "mm[Hg]",
            "°C" => "Cel",
            "mS/cm" => "mS/cm",
            "bpm" => "/min",
            _ => unit
        };
    }
}
