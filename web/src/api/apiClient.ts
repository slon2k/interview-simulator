import axios from 'axios'

export const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
  // repeat array params as `key=a&key=b` (default axios `key[]=a&key[]=b` isn't bound by ASP.NET Core)
  paramsSerializer: { indexes: null },
})
