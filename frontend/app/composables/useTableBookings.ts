export const useTableBookings = () => {
  const config = useRuntimeConfig();
  const apiBase = config.public.apiBase;
  const { apiFetch } = useApi();

  interface TableBookingDto {
    bookingId: string;
    timestamp?: string;
    contactName?: string;
    contactPhone?: string;
    guestCount?: number;
    status?: string;
    hasComment?: boolean;
    detailUrl?: string;
  }

  interface TableBookingListDto {
    bookings: TableBookingDto[];
    totalCount: number;
    page: number;
    pageSize: number;
    hasMorePages: boolean;
  }

  const getTableBookings = async (
    fromDate?: Date,
    toDate?: Date,
    contactName?: string,
    contactPhone?: string,
    statusId?: number,
    page: number = 1,
    pageSize: number = 50
  ): Promise<TableBookingListDto> => {
    const params = new URLSearchParams();
    
    if (fromDate) {
      params.append('fromDate', fromDate.toISOString().split('T')[0]);
    }
    if (toDate) {
      params.append('toDate', toDate.toISOString().split('T')[0]);
    }
    if (contactName) {
      params.append('contactName', contactName);
    }
    if (contactPhone) {
      params.append('contactPhone', contactPhone);
    }
    if (statusId !== undefined && statusId !== null) {
      params.append('statusId', statusId.toString());
    }
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    const url = `/api/bookings/table?${params.toString()}`;
    const data = await apiFetch(url);
    return data;
  };

  return {
    getTableBookings
  };
};

