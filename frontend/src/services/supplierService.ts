import apiClient from './api';
import type { SupplierSummaryDto } from '../types/api';

export const supplierService = {
  // Get supplier summary
  async getSupplierSummary(id: string): Promise<SupplierSummaryDto> {
    const response = await apiClient.get(`/api/Suppliers/${id}/summary`);
    return response.data;
  },
};
