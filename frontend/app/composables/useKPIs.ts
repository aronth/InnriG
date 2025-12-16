export interface KPIValue {
  current: number
  target: number
  status: string
  details: string
}

export interface QuarterStatusDto {
  updated: number
  total: number
  percentage: number
}

export interface QuarterlyBreakdownDto {
  q1: QuarterStatusDto
  q2: QuarterStatusDto
  q3: QuarterStatusDto
  q4: QuarterStatusDto
}

export interface KPIsDto {
  productListCompleteness: KPIValue
  staleProducts: KPIValue
  quarterlyUpdateFrequency: KPIValue
  usageMetrics: KPIValue
  quarterlyBreakdown: QuarterlyBreakdownDto
}

export const useKPIs = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getKPIs = async (): Promise<KPIsDto> => {
    return await apiFetch<KPIsDto>(`${apiBase}/api/kpis`)
  }

  return {
    getKPIs
  }
}

