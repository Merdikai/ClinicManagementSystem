using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Application.Medicines.Commands;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Application.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        // ─── Patient ───
        CreateMap<CreatePatientDto, Patient>();
        CreateMap<Patient, PatientResponseDto>();
        CreateMap<CreatePatientDto, CreatePatientCommand>();

        // ─── Appointment ───
        CreateMap<CreateAppointmentDto, Appointment>();
        CreateMap<Appointment, AppointmentResponseDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"{src.Doctor.FirstName} {src.Doctor.LastName}"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.VitalSigns, opt => opt.MapFrom(src => src.VitalSign))
            .ForMember(dest => dest.TokenNumber, opt => opt.MapFrom(src => src.TokenNumber))
            .ForMember(dest => dest.VisitType, opt => opt.MapFrom(src => src.VisitType));

        // ─── VitalSign ───
        CreateMap<RecordVitalsDto, VitalSign>();
        CreateMap<VitalSign, VitalSignResponseDto>()
            .ForMember(dest => dest.RecordedByUser, opt => opt.MapFrom(src => $"{src.RecordedByUser.FirstName} {src.RecordedByUser.LastName}"));

        // ─── Consultation ───
        CreateMap<CreateConsultationDto, Consultation>();
        CreateMap<Consultation, ConsultationResponseDto>()
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"{src.Doctor.FirstName} {src.Doctor.LastName}"))
            .ForMember(dest => dest.Prescription, opt => opt.MapFrom(src => src.Prescription));

        // ─── Prescription ───
        CreateMap<Prescription, PrescriptionResponseDto>();
        CreateMap<PrescriptionItem, PrescriptionItemResponseDto>()
            .ForMember(dest => dest.MedicineName, opt => opt.MapFrom(src => src.Medicine.Name))
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

        // ─── Medicine ───
        CreateMap<CreateMedicineDto, Medicine>();
        CreateMap<Medicine, MedicineResponseDto>();
        CreateMap<CreateMedicineDto, CreateMedicineCommand>();

        // ─── Invoice ───
        CreateMap<CreateInvoiceDto, CreateInvoiceCommand>();
        CreateMap<Invoice, InvoiceResponseDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.LineItems, opt => opt.MapFrom(src => src.LineItems))
            .ForMember(dest => dest.BalanceDue, opt => opt.MapFrom(src => src.BalanceDue));

        CreateMap<InvoiceItem, InvoiceItemResponseDto>()
            .ForMember(dest => dest.LineTotal, opt => opt.MapFrom(src => src.LineTotal));

        CreateMap<CreateInvoiceItemDto, InvoiceItem>();

        // ─── Payment ───
        CreateMap<Payment, PaymentResponseDto>();

        // ─── Nursing Procedure ───
        CreateMap<NursingProcedure, NursingProcedureResponseDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient != null ? $"{src.Patient.FirstName} {src.Patient.LastName}" : string.Empty))
            .ForMember(dest => dest.PerformedByUserName, opt => opt.MapFrom(src => src.PerformedByUser != null ? $"{src.PerformedByUser.FirstName} {src.PerformedByUser.LastName}" : string.Empty))
            .ForMember(dest => dest.OrderedByDoctorName, opt => opt.MapFrom(src => src.OrderedByDoctor != null ? $"{src.OrderedByDoctor.FirstName} {src.OrderedByDoctor.LastName}" : string.Empty));

        // ─── Shift Handover ───
        CreateMap<ShiftHandover, ShiftHandoverResponseDto>()
            .ForMember(dest => dest.NurseUserName, opt => opt.MapFrom(src => src.NurseUser != null ? $"{src.NurseUser.FirstName} {src.NurseUser.LastName}" : string.Empty));

        // ─── Daily Cash Reconciliation ───
        CreateMap<DailyCashReconciliation, DailyCashReconciliationResponseDto>()
            .ForMember(dest => dest.ReceptionistName, opt => opt.MapFrom(src => src.ReceptionistUser != null ? $"{src.ReceptionistUser.FirstName} {src.ReceptionistUser.LastName}" : "Front Desk"));

        // ─── Visitor Inquiry ───
        CreateMap<VisitorInquiry, VisitorInquiryResponseDto>()
            .ForMember(dest => dest.HandledByUserName, opt => opt.MapFrom(src => src.HandledByUser != null ? $"{src.HandledByUser.FirstName} {src.HandledByUser.LastName}" : "Front Desk"));
    }
}
