export const useBookings = () => {
  const config = useRuntimeConfig();
  const apiBase = config.public.apiBase;
  const { apiFetch } = useApi();

  const getWeekBookings = async (unixTimestamp?: number) => {
    const url = unixTimestamp 
      ? `/api/bookings/week?dt=${unixTimestamp}`
      : `/api/bookings/week`;
    
    const data = await apiFetch(url);
    return data;
  };

  const clearCache = async () => {
    const data = await apiFetch(`/api/bookings/clear-cache`, {
      method: 'POST'
    });
    return data;
  };

  return {
    getWeekBookings,
    clearCache
  };
};

