import CrudPage from '../components/CrudPage'

const fields = [
  { name: 'provinceId', label: 'Province Id', type: 'int', required: true },
  { name: 'cityCode', label: 'City Code', type: 'text', required: true },
  { name: 'cityName', label: 'City Name', type: 'text', required: true },
  {
    name: 'cityType',
    label: 'City Type',
    type: 'select',
    options: ['City', 'Municipality'],
    required: true,
  },
  { name: 'createdAt', label: 'Created At', type: 'datetime-local', inForm: false },
]

export default function CitiesPage() {
  return <CrudPage title="Cities" endpoint="/Cities" idField="cityId" fields={fields} />
}
