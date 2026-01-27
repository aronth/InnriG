export interface Shift {
    startTime: string
    endTime: string
    duration: string
    workLocation?: string | null
    type?: string | null
    approved?: string | null
    clockInNote?: string | null
    dayNote?: string | null
    groupNumber?: string | null
    group?: string | null
}

export interface EmployeeTimeReport {
    name: string
    kennitala: string
    shifts: Shift[]
    totalShifts: number
    totalHours?: string | null
}

export interface TimeReportParseResult {
    employees: EmployeeTimeReport[]
    totalShifts: number
    reportStartDate?: string | null
    reportEndDate?: string | null
}

export const useTimeReports = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const parseTimeReport = async (file: File): Promise<TimeReportParseResult> => {
        const formData = new FormData()
        formData.append('file', file)

        return await apiFetch<TimeReportParseResult>(`${apiBase}/api/timereports/parse`, {
            method: 'POST',
            body: formData
        })
    }

    return {
        parseTimeReport
    }
}

