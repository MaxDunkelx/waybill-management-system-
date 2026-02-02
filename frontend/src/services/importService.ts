import apiClient from './api';
import type { ImportResult } from '../types/api';

export const importService = {
  // Import waybills from CSV file
  async importWaybills(file: File): Promise<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post('/api/WaybillImport/import', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return response.data;
  },
};
