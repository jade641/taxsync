import CrudPage from '../components/CrudPage'

const fields = [
  { name: 'regionId', label: 'Region Id', type: 'int', required: true },
  { name: 'provinceCode', label: 'Province Code', type: 'text', required: true },
  { name: 'provinceName', label: 'Province Name', type: 'text', required: true },
  { name: 'createdAt', label: 'Created At', type: 'datetime-local', inForm: false },
]

export default function ProvincesPage() {
  return <CrudPage title="Provinces" endpoint="/Provinces" idField="provinceId" fields={fields} />
}
