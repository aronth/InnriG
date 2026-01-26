<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-center gap-3">
        <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Skýrslur</h1>
          <p class="text-sm text-gray-600">Sérsniðnar skýrslur um pantanir</p>
        </div>
      </div>
    </div>

    <!-- Subnav -->
    <OrderSubNav />

    <!-- Filters -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
      <h2 class="text-lg font-semibold text-gray-800 mb-4">Síur</h2>
      <div class="grid grid-cols-1 md:grid-cols-5 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Frá</label>
          <input 
            v-model="filters.fromDate" 
            type="date" 
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all" 
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Til</label>
          <input 
            v-model="filters.toDate" 
            type="date" 
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all" 
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Afgreiðsla</label>
          <select 
            v-model="filters.deliveryMethod" 
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option value="">Allar</option>
            <option value="Sent">Sent</option>
            <option value="Sótt">Sótt</option>
            <option value="Salur">Salur</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Veitingastaður</label>
          <select 
            v-model="filters.restaurantId" 
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option :value="null">Allir</option>
            <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">
              {{ restaurant.name }}
            </option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Tímabil</label>
          <select 
            v-model="filters.granularity" 
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option value="day">Dagur</option>
            <option value="week">Vika</option>
            <option value="month">Mánuður</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Report Type Selector -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
      <h2 class="text-lg font-semibold text-gray-800 mb-4">Veldu skýrslugerð</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <button
          v-for="reportType in reportTypes"
          :key="reportType.id"
          @click="selectedReportType = reportType.id"
          :class="[
            'p-4 rounded-xl border-2 transition-all text-left',
            selectedReportType === reportType.id
              ? 'border-indigo-500 bg-indigo-50 shadow-md'
              : 'border-gray-200 hover:border-indigo-300 hover:bg-gray-50'
          ]"
        >
          <div class="flex items-start gap-3">
            <div :class="[
              'w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0',
              selectedReportType === reportType.id ? 'bg-indigo-600' : 'bg-gray-200'
            ]">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="reportType.icon" />
              </svg>
            </div>
            <div class="flex-1">
              <h3 class="font-semibold text-gray-900 mb-1">{{ reportType.name }}</h3>
              <p class="text-sm text-gray-600">{{ reportType.description }}</p>
            </div>
          </div>
        </button>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
      {{ error }}
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="p-10 flex justify-center">
      <div class="relative">
        <div class="w-16 h-16 border-4 border-indigo-200 rounded-full"></div>
        <div class="w-16 h-16 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
      </div>
    </div>

    <!-- Report Display -->
    <div v-else-if="selectedReportType && !isLoading" class="space-y-6">
      <!-- Summary Dashboard -->
      <div v-if="selectedReportType === 'summary'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Yfirlit</h2>
        <div v-if="summaryData" class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
          <div class="bg-gradient-to-br from-indigo-50 to-indigo-100 rounded-xl p-4">
            <p class="text-sm text-gray-600">Samtals pantanir</p>
            <p class="text-2xl font-bold text-gray-900">{{ summaryData.totalOrders.toLocaleString('is-IS') }}</p>
          </div>
          <div class="bg-gradient-to-br from-purple-50 to-purple-100 rounded-xl p-4">
            <p class="text-sm text-gray-600">Mælanlegar</p>
            <p class="text-2xl font-bold text-gray-900">{{ summaryData.evaluableOrders.toLocaleString('is-IS') }}</p>
          </div>
          <div class="bg-gradient-to-br from-red-50 to-red-100 rounded-xl p-4">
            <p class="text-sm text-gray-600">Seinar</p>
            <p class="text-2xl font-bold text-gray-900">{{ summaryData.lateOrders.toLocaleString('is-IS') }}</p>
          </div>
          <div class="bg-gradient-to-br from-yellow-50 to-yellow-100 rounded-xl p-4">
            <p class="text-sm text-gray-600">Meðal biðtími</p>
            <p class="text-2xl font-bold text-gray-900">
              {{ summaryData.avgWaitTimeMin ? `${Math.round(summaryData.avgWaitTimeMin)} mín` : '-' }}
            </p>
          </div>
          <div class="bg-gradient-to-br from-green-50 to-green-100 rounded-xl p-4">
            <p class="text-sm text-gray-600">P90 biðtími</p>
            <p class="text-2xl font-bold text-gray-900">
              {{ summaryData.p90WaitTimeMin ? `${Math.round(summaryData.p90WaitTimeMin)} mín` : '-' }}
            </p>
          </div>
          <div class="bg-gradient-to-br from-blue-50 to-blue-100 rounded-xl p-4">
            <p class="text-sm text-gray-600">Heildartekjur</p>
            <p class="text-2xl font-bold text-gray-900">
              {{ formatCurrency(summaryData.totalRevenue) }}
            </p>
          </div>
        </div>
      </div>

      <!-- Wait Time Series -->
      <div v-if="selectedReportType === 'wait-time'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Biðtími röð</h2>
        <div v-if="waitTimeSeriesData && waitTimeSeriesData.length > 0" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Tímabil</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Fjöldi</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Meðal biðtími</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">P90 biðtími</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr v-for="point in waitTimeSeriesData" :key="point.periodStart" class="hover:bg-indigo-50">
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{{ formatDate(point.periodStart) }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">{{ point.count }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                  {{ point.avgWaitTimeMin ? `${Math.round(point.avgWaitTimeMin)} mín` : '-' }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                  {{ point.p90WaitTimeMin ? `${Math.round(point.p90WaitTimeMin)} mín` : '-' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="text-center text-gray-500 py-8">Engin gögn fundust</div>
      </div>

      <!-- Late Orders Series -->
      <div v-if="selectedReportType === 'late-orders'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Seinar pantanir</h2>
        <div v-if="lateSeriesData && lateSeriesData.length > 0" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Tímabil</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Samtals</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Mælanlegar</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Seinar</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Hlutfall</th>
            </tr>
          </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr v-for="point in lateSeriesData" :key="point.periodStart" class="hover:bg-indigo-50">
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{{ formatDate(point.periodStart) }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">{{ point.total }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">{{ point.evaluable }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-bold text-red-600 text-right">{{ point.lateCount }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">{{ formatPercent(point.lateRatio) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
        <div v-else class="text-center text-gray-500 py-8">Engin gögn fundust</div>
    </div>

      <!-- Heatmaps -->
      <div v-if="selectedReportType === 'heatmaps'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-xl font-bold text-gray-800">Hitakort</h2>
            <p class="text-sm text-gray-600 mt-1">Hitakort eftir vikudegi og klukkustund (10:00 - 23:00)</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">Tegund hitakorts</label>
            <select 
              v-model="selectedHeatmapType" 
              @change="loadReportData"
              class="px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
            >
              <option value="volume">Magn pantana</option>
              <option value="late">Hlutfall seina</option>
              <option value="waittime">Meðal biðtími</option>
              <option value="p90-waittime">P90 biðtími</option>
            </select>
          </div>
        </div>
        <div v-if="heatmapData && heatmapData.cells && heatmapData.cells.length > 0" class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr>
                <th class="px-2 py-2 text-left text-xs font-medium text-gray-700">Vikudagur</th>
                <th v-for="hour in 14" :key="hour" class="px-2 py-2 text-center text-xs font-medium text-gray-700">
                  {{ hour + 9 }}:00
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, weekday) in heatmapData.cells" :key="weekday" class="hover:bg-gray-50">
                <td class="px-2 py-2 font-medium text-gray-900">
                  {{ ['Mán', 'Þri', 'Mið', 'Fim', 'Fös', 'Lau', 'Sun'][weekday] }}
                </td>
                <td v-for="hour in 14" :key="hour" 
                    :class="[
                      'px-2 py-2 text-center text-xs font-medium',
                      getHeatmapColorForCell(heatmapData.cells, weekday, hour + 9, selectedHeatmapType)
                    ]">
                  {{ formatHeatmapValue(heatmapData.cells, weekday, hour + 9, selectedHeatmapType) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="text-center text-gray-500 py-8">Engin gögn fundust</div>
      </div>

      <!-- Growth Metrics -->
      <div v-if="selectedReportType === 'growth'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Vöxtur eftir viku</h2>
        <div v-if="growthData && growthData.length > 0" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Vika</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Pantanir</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Vöxtur</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Seinar</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Vöxtur</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Meðal biðtími</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase">Vöxtur</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr v-for="week in growthData" :key="week.weekStart" class="hover:bg-indigo-50">
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {{ formatDate(week.weekStart) }} - {{ formatDate(week.weekEnd) }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">{{ week.totalOrders }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-right" :class="getGrowthColor(week.totalOrdersGrowth)">
                  {{ formatGrowth(week.totalOrdersGrowth) }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">{{ week.lateOrders }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-right" :class="getGrowthColor(week.lateOrdersGrowth)">
                  {{ formatGrowth(week.lateOrdersGrowth) }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                  {{ week.avgWaitTimeMin ? `${Math.round(week.avgWaitTimeMin)} mín` : '-' }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-right" :class="getGrowthColor(week.avgWaitTimeGrowth)">
                  {{ formatGrowth(week.avgWaitTimeGrowth) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="text-center text-gray-500 py-8">Engin gögn fundust</div>
      </div>

      <!-- Period Comparison -->
      <div v-if="selectedReportType === 'comparison'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Samanburður tímabila</h2>
        <div class="mb-4 p-4 bg-yellow-50 border border-yellow-200 rounded-lg text-sm text-yellow-800">
          <p><strong>Athugið:</strong> Veldu tímabil með því að stilla "Frá" dagsetninguna. Samanburðurinn bera saman núverandi tímabil við fyrra tímabil (vika eða mánuður).</p>
        </div>
        <div v-if="comparisonData" class="space-y-6">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="bg-gradient-to-br from-indigo-50 to-indigo-100 rounded-xl p-6">
              <h3 class="font-semibold text-gray-800 mb-4">Núverandi tímabil</h3>
              <div class="space-y-2 text-sm">
                <div class="flex justify-between">
                  <span class="text-gray-600">Pantanir:</span>
                  <span class="font-bold">{{ comparisonData.currentPeriod.totalOrders.toLocaleString('is-IS') }}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Tekjur:</span>
                  <span class="font-bold">{{ formatCurrency(comparisonData.currentPeriod.totalRevenue) }}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Seinar:</span>
                  <span class="font-bold">{{ comparisonData.currentPeriod.lateOrders }}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Meðal biðtími:</span>
                  <span class="font-bold">
                    {{ comparisonData.currentPeriod.avgWaitTimeMin ? `${Math.round(comparisonData.currentPeriod.avgWaitTimeMin)} mín` : '-' }}
                  </span>
                </div>
              </div>
            </div>
            <div class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-xl p-6">
              <h3 class="font-semibold text-gray-800 mb-4">Fyrra tímabil</h3>
              <div class="space-y-2 text-sm">
                <div class="flex justify-between">
                  <span class="text-gray-600">Pantanir:</span>
                  <span class="font-bold">{{ comparisonData.previousPeriod.totalOrders.toLocaleString('is-IS') }}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Tekjur:</span>
                  <span class="font-bold">{{ formatCurrency(comparisonData.previousPeriod.totalRevenue) }}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Seinar:</span>
                  <span class="font-bold">{{ comparisonData.previousPeriod.lateOrders }}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Meðal biðtími:</span>
                  <span class="font-bold">
                    {{ comparisonData.previousPeriod.avgWaitTimeMin ? `${Math.round(comparisonData.previousPeriod.avgWaitTimeMin)} mín` : '-' }}
                  </span>
                </div>
              </div>
            </div>
          </div>
          <div class="bg-white border-2 border-indigo-200 rounded-xl p-6">
            <h3 class="font-semibold text-gray-800 mb-4">Breytingar</h3>
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div>
                <p class="text-sm text-gray-600">Pantanir</p>
                <p class="text-xl font-bold" :class="getChangeColor(comparisonData.comparison.ordersChangePercent)">
                  {{ formatPercentChange(comparisonData.comparison.ordersChangePercent) }}
                </p>
              </div>
              <div>
                <p class="text-sm text-gray-600">Tekjur</p>
                <p class="text-xl font-bold" :class="getChangeColor(comparisonData.comparison.revenueChangePercent)">
                  {{ formatPercentChange(comparisonData.comparison.revenueChangePercent) }}
                </p>
              </div>
              <div>
                <p class="text-sm text-gray-600">Seinar hlutfall</p>
                <p class="text-xl font-bold" :class="getChangeColor(comparisonData.comparison.lateRatioChangePercent)">
                  {{ formatPercentChange(comparisonData.comparison.lateRatioChangePercent) }}
                </p>
              </div>
              <div>
                <p class="text-sm text-gray-600">Biðtími</p>
                <p class="text-xl font-bold" :class="getChangeColor(comparisonData.comparison.waitTimeChangePercent)">
                  {{ formatPercentChange(comparisonData.comparison.waitTimeChangePercent) }}
                </p>
              </div>
            </div>
          </div>
        </div>
        <div v-else class="text-center text-gray-500 py-8">Veldu tímabil með "Frá" dagsetningu til að sjá samanburð</div>
      </div>

      <!-- Forecast -->
      <div v-if="selectedReportType === 'forecast'" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Spá</h2>
        <div class="mb-4 p-4 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-800">
          <p><strong>Athugið:</strong> Veldu tímabil með því að stilla "Frá" dagsetninguna. Spáin byggir á sögulegum gögnum.</p>
        </div>
        <div v-if="forecastData" class="space-y-6">
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div class="bg-gradient-to-br from-indigo-50 to-indigo-100 rounded-xl p-6">
              <p class="text-sm text-gray-600 mb-2">Spáðar pantanir</p>
              <p class="text-3xl font-bold text-gray-900">{{ forecastData.predictedOrders.toLocaleString('is-IS') }}</p>
              <p class="text-xs text-gray-500 mt-1">
                {{ forecastData.predictedOrdersMin }} - {{ forecastData.predictedOrdersMax }}
              </p>
            </div>
            <div class="bg-gradient-to-br from-green-50 to-green-100 rounded-xl p-6">
              <p class="text-sm text-gray-600 mb-2">Spáðar tekjur</p>
              <p class="text-3xl font-bold text-gray-900">{{ formatCurrency(forecastData.predictedRevenue) }}</p>
              <p class="text-xs text-gray-500 mt-1">
                {{ formatCurrency(forecastData.predictedRevenueMin) }} - {{ formatCurrency(forecastData.predictedRevenueMax) }}
              </p>
            </div>
            <div class="bg-gradient-to-br from-purple-50 to-purple-100 rounded-xl p-6">
              <p class="text-sm text-gray-600 mb-2">Spáð hlutfall seina</p>
              <p class="text-3xl font-bold text-gray-900">{{ formatPercent(forecastData.predictedLateRatio) }}</p>
              <p class="text-xs text-gray-500 mt-1">
                Meðal biðtími: {{ forecastData.predictedAvgWaitTime ? `${Math.round(forecastData.predictedAvgWaitTime)} mín` : '-' }}
              </p>
            </div>
          </div>
          <div class="bg-white border border-gray-200 rounded-xl p-6">
            <h3 class="font-semibold text-gray-800 mb-4">Eftir vikudegi</h3>
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-700 uppercase">Vikudagur</th>
                    <th class="px-4 py-3 text-right text-xs font-medium text-gray-700 uppercase">Dagar</th>
                    <th class="px-4 py-3 text-right text-xs font-medium text-gray-700 uppercase">Spáðar pantanir</th>
                    <th class="px-4 py-3 text-right text-xs font-medium text-gray-700 uppercase">Spáðar tekjur</th>
            </tr>
          </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                  <tr v-for="day in forecastData.byWeekday" :key="day.weekday" class="hover:bg-gray-50">
                    <td class="px-4 py-3 text-sm font-medium text-gray-900">{{ day.weekdayName }}</td>
                    <td class="px-4 py-3 text-sm text-gray-900 text-right">{{ day.daysInPeriod }}</td>
                    <td class="px-4 py-3 text-sm text-gray-900 text-right">{{ day.predictedOrders }}</td>
                    <td class="px-4 py-3 text-sm text-gray-900 text-right">{{ formatCurrency(day.predictedRevenue) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
          </div>
        </div>
        <div v-else class="text-center text-gray-500 py-8">Veldu tímabil með "Frá" dagsetningu til að sjá spá</div>
      </div>

      <!-- Timeline -->
      <div v-if="selectedReportType === 'timeline'" class="space-y-6">
        <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
          <div class="mb-4 p-4 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-800">
            <p><strong>Athugið:</strong> Tímalínan sýnir pantanir fyrir einn dag. Veldu "Frá" dagsetningu til að sjá pantanir fyrir þann dag.</p>
          </div>
          <div v-if="timelineData && timelineData.orders.length > 0">
            <h2 class="text-xl font-bold text-gray-800 mb-4">Tímamælingar</h2>
            <OrderTimelineChart :timeline-data="timelineData" />
          </div>
          <div v-else class="text-center text-gray-500 py-8">Engar pantanir fundust fyrir valinn dag</div>
        </div>

        <!-- Timeline Data Table -->
        <div v-if="timelineData && timelineData.orders.length > 0" class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
          <div class="p-6 border-b border-gray-200">
            <h2 class="text-lg font-semibold text-gray-900">Ítarlegar upplýsingar</h2>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gradient-to-r from-indigo-50 to-purple-50 border-b border-gray-200">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Pöntunartími
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Pöntunarnúmer
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Afhending
                  </th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Biðtími
                  </th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Skönnun
                  </th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Útskráning
                  </th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                    Upphæð
                  </th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr v-for="order in timelineData.orders" :key="order.orderId" class="hover:bg-indigo-50">
                  <td class="px-6 py-4 whitespace-nowrap text-gray-900">
                    {{ formatTime(order.orderTime) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-gray-600">
                    {{ order.orderNumber || '-' }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span :class="getDeliveryMethodClass(order.deliveryMethod)">
                      {{ order.deliveryMethod }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                    {{ order.waitTimeMinutes != null ? `${order.waitTimeMinutes} mín` : '-' }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                    {{ order.timeToScanMinutes != null ? `${Math.round(order.timeToScanMinutes)} mín` : '-' }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                    {{ order.timeToCheckoutMinutes != null ? `${Math.round(order.timeToCheckoutMinutes)} mín` : '-' }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                    {{ order.totalAmount != null ? formatCurrency(order.totalAmount) : '-' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- No Report Selected -->
    <div v-else class="bg-white rounded-2xl shadow-lg border border-gray-100 p-12 text-center">
      <div class="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      </div>
      <h3 class="text-lg font-semibold text-gray-700 mb-2">Veldu skýrslugerð</h3>
      <p class="text-gray-500">Veldu skýrslugerð hér að ofan til að sjá gögn</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { 
  OrderReportSummaryDto, 
  OrderWaitTimeSeriesPointDto, 
  OrderLateRatioPointDto,
  OrderHeatmapDto,
  OrderHeatmapCellDto,
  OrderGrowthWeekDto,
  PeriodComparisonDto,
  ForecastDto,
  OrderTimelineReportDto
} from '~/app/composables/useOrders'

const {
  getSummary,
  getWaitTimeSeries,
  getLateSeries,
  getVolumeHeatmap,
  getLateHeatmap,
  getWaitTimeHeatmap,
  getP90WaitTimeHeatmap,
  getGrowthByWeek,
  getPeriodComparison,
  getForecast,
  getTimeline
} = useOrders()

const config = useRuntimeConfig()
const apiBase = config.public.apiBase
const { apiFetch } = useApi()

type Restaurant = {
  id: string
  name: string
  code: string
}

type ReportType = {
  id: string
  name: string
  description: string
  icon: string
}

const reportTypes: ReportType[] = [
  {
    id: 'summary',
    name: 'Yfirlit',
    description: 'Heildaryfirlit með KPIs',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z'
  },
  {
    id: 'wait-time',
    name: 'Biðtími',
    description: 'Biðtími röð eftir tímabili',
    icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z'
  },
  {
    id: 'late-orders',
    name: 'Seinar pantanir',
    description: 'Hlutfall seina pantana',
    icon: 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
  },
  {
    id: 'heatmaps',
    name: 'Hitakort',
    description: 'Hitakort eftir vikudegi og tíma',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z'
  },
  {
    id: 'growth',
    name: 'Vöxtur',
    description: 'Vöxtur eftir viku',
    icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6'
  },
  {
    id: 'comparison',
    name: 'Samanburður',
    description: 'Samanburður tímabila',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z'
  },
  {
    id: 'forecast',
    name: 'Spá',
    description: 'Spá fyrir framtíð',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z'
  },
  {
    id: 'timeline',
    name: 'Tímalína',
    description: 'Tímamælingar pantana eftir degi',
    icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z'
  }
]

const today = new Date()
const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)

const filters = ref({
  fromDate: thirtyDaysAgo.toISOString().split('T')[0],
  toDate: today.toISOString().split('T')[0],
  deliveryMethod: '',
  restaurantId: null as string | null,
  granularity: 'day' as 'day' | 'week' | 'month'
})

const selectedReportType = ref<string | null>(null)
const restaurants = ref<Restaurant[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

const summaryData = ref<OrderReportSummaryDto | null>(null)
const waitTimeSeriesData = ref<OrderWaitTimeSeriesPointDto[]>([])
const lateSeriesData = ref<OrderLateRatioPointDto[]>([])
const heatmapData = ref<OrderHeatmapDto | null>(null)
const selectedHeatmapType = ref<'volume' | 'late' | 'waittime' | 'p90-waittime'>('volume')
const growthData = ref<OrderGrowthWeekDto[]>([])
const comparisonData = ref<PeriodComparisonDto | null>(null)
const forecastData = ref<ForecastDto | null>(null)
const timelineData = ref<OrderTimelineReportDto | null>(null)

const formatDate = (iso: string) => {
  try {
    return new Date(iso).toLocaleDateString('is-IS')
  } catch {
    return iso
  }
}

const formatPercent = (value: number) => {
  return new Intl.NumberFormat('is-IS', { style: 'percent', maximumFractionDigits: 1 }).format(value)
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('is-IS', { 
    style: 'currency', 
    currency: 'ISK',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value)
}

const formatTime = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleTimeString('is-IS', { hour: '2-digit', minute: '2-digit' })
}

const getDeliveryMethodClass = (method: string): string => {
  const classes: Record<string, string> = {
    'Sótt': 'px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800',
    'Sent': 'px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800',
    'Salur': 'px-2 py-1 text-xs font-medium rounded-full bg-purple-100 text-purple-800'
  }
  return classes[method] || 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-800'
}

const formatGrowth = (value: number | null | undefined) => {
  if (value === null || value === undefined) return '-'
  const sign = value >= 0 ? '+' : ''
  return `${sign}${value.toFixed(1)}%`
}

const formatPercentChange = (value: number | null | undefined) => {
  if (value === null || value === undefined) return '-'
  const sign = value >= 0 ? '+' : ''
  return `${sign}${value.toFixed(1)}%`
}

const getGrowthColor = (value: number | null | undefined) => {
  if (value === null || value === undefined) return 'text-gray-500'
  return value >= 0 ? 'text-green-600' : 'text-red-600'
}

const getChangeColor = (value: number | null | undefined) => {
  if (value === null || value === undefined) return 'text-gray-500'
  return value >= 0 ? 'text-green-600' : 'text-red-600'
}

const getCellValue = (cells: OrderHeatmapCellDto[][], weekday: number, hour: number): number => {
  try {
    if (!cells || !cells[weekday]) return 0
    const cell = cells[weekday].find(c => c.hour === hour)
    return cell?.value || 0
  } catch (e) {
    return 0
  }
}

const formatHeatmapValue = (cells: OrderHeatmapCellDto[][], weekday: number, hour: number, type: string): string => {
  const value = getCellValue(cells, weekday, hour)
  if (value === 0) return '0'
  
  if (type === 'late') {
    return formatPercent(value)
  } else if (type === 'waittime' || type === 'p90-waittime') {
    return `${Math.round(value)} mín`
  } else {
    return value.toString()
  }
}

const getHeatmapColorForCell = (cells: OrderHeatmapCellDto[][], weekday: number, hour: number, type: string): string => {
  try {
    if (!cells || cells.length === 0) return 'bg-gray-100'
    
    // Get all values from hours 10-23 only
    const allValues: number[] = []
    for (let wd = 0; wd < cells.length; wd++) {
      if (cells[wd]) {
        for (const cell of cells[wd]) {
          if (cell.hour >= 10 && cell.hour <= 23) {
            allValues.push(cell.value || 0)
          }
        }
      }
    }
    
    const maxValue = allValues.length > 0 ? Math.max(...allValues) : 0
    if (maxValue === 0) return 'bg-gray-100'
    
    const cellValue = getCellValue(cells, weekday, hour)
    const intensity = Math.min(cellValue / maxValue, 1)
    
    // Different color schemes for different heatmap types
    if (type === 'late') {
      // Red gradient for late ratio (higher is worse)
      if (intensity === 0) return 'bg-gray-50 text-gray-500'
      if (intensity < 0.1) return 'bg-red-100 text-gray-700'
      if (intensity < 0.2) return 'bg-red-200 text-gray-700'
      if (intensity < 0.4) return 'bg-red-300 text-gray-800'
      if (intensity < 0.6) return 'bg-red-400 text-white'
      if (intensity < 0.8) return 'bg-red-500 text-white'
      return 'bg-red-600 text-white font-bold'
    } else if (type === 'waittime' || type === 'p90-waittime') {
      // Orange/yellow gradient for wait times (higher is worse)
      if (intensity === 0) return 'bg-gray-50 text-gray-500'
      if (intensity < 0.1) return 'bg-yellow-100 text-gray-700'
      if (intensity < 0.2) return 'bg-yellow-200 text-gray-700'
      if (intensity < 0.4) return 'bg-orange-300 text-gray-800'
      if (intensity < 0.6) return 'bg-orange-400 text-white'
      if (intensity < 0.8) return 'bg-orange-500 text-white'
      return 'bg-orange-600 text-white font-bold'
    } else {
      // Blue gradient for volume (higher is better)
      if (intensity === 0) return 'bg-gray-50 text-gray-500'
      if (intensity < 0.1) return 'bg-blue-100 text-gray-700'
      if (intensity < 0.2) return 'bg-blue-200 text-gray-700'
      if (intensity < 0.4) return 'bg-blue-300 text-gray-800'
      if (intensity < 0.6) return 'bg-blue-400 text-white'
      if (intensity < 0.8) return 'bg-blue-500 text-white'
      return 'bg-blue-600 text-white font-bold'
    }
  } catch (e) {
    console.error('Error calculating heatmap color:', e)
    return 'bg-gray-100'
  }
}

const loadRestaurants = async () => {
  try {
    restaurants.value = await apiFetch<Restaurant[]>(`${apiBase}/api/restaurants`)
  } catch (e: any) {
    console.error('Failed to load restaurants:', e)
  }
}

const loadReportData = async () => {
  if (!selectedReportType.value) return

  isLoading.value = true
  error.value = null

  try {
    const params: any = {
      from: filters.value.fromDate,
      to: filters.value.toDate
    }

    if (filters.value.deliveryMethod) {
      params.deliveryMethod = filters.value.deliveryMethod
    }

    if (filters.value.restaurantId) {
      params.restaurantId = filters.value.restaurantId
    }

    // Clear all data first
    summaryData.value = null
    waitTimeSeriesData.value = []
    lateSeriesData.value = []
    heatmapData.value = null
    growthData.value = []
    comparisonData.value = null
    forecastData.value = null
    timelineData.value = null

    switch (selectedReportType.value) {
      case 'summary':
        summaryData.value = await getSummary(params)
        break
      case 'wait-time':
        params.granularity = filters.value.granularity
        waitTimeSeriesData.value = await getWaitTimeSeries(params)
        break
      case 'late-orders':
        params.granularity = filters.value.granularity
        lateSeriesData.value = await getLateSeries(params)
        break
      case 'heatmaps':
        switch (selectedHeatmapType.value) {
          case 'volume':
            heatmapData.value = await getVolumeHeatmap(params)
            break
          case 'late':
            heatmapData.value = await getLateHeatmap(params)
            break
          case 'waittime':
            heatmapData.value = await getWaitTimeHeatmap(params)
            break
          case 'p90-waittime':
            heatmapData.value = await getP90WaitTimeHeatmap(params)
            break
        }
        break
      case 'growth':
        growthData.value = await getGrowthByWeek({
          from: filters.value.fromDate,
          to: filters.value.toDate,
          deliveryMethod: filters.value.deliveryMethod || undefined,
          restaurantId: filters.value.restaurantId || undefined
        })
        break
      case 'comparison':
        // For comparison, we need periodType and periodStart
        // Use the fromDate as periodStart and determine periodType from granularity
        const periodType = filters.value.granularity === 'month' ? 'month' : 'week'
        comparisonData.value = await getPeriodComparison({
          periodType,
          periodStart: filters.value.fromDate,
          deliveryMethod: filters.value.deliveryMethod || undefined,
          restaurantId: filters.value.restaurantId || undefined
        })
        break
      case 'forecast':
        const forecastPeriodType = filters.value.granularity === 'month' ? 'month' : 'week'
        forecastData.value = await getForecast({
          periodType: forecastPeriodType,
          targetPeriodStart: filters.value.fromDate,
          deliveryMethod: filters.value.deliveryMethod || undefined,
          restaurantId: filters.value.restaurantId || undefined
        })
        break
      case 'timeline':
        timelineData.value = await getTimeline({
          date: filters.value.fromDate,
          deliveryMethod: filters.value.deliveryMethod || undefined,
          restaurantId: filters.value.restaurantId || undefined
        })
        break
    }
  } catch (e: any) {
    console.error('Error loading report data:', e)
    error.value = e?.data?.message || e?.message || 'Villa kom upp við að sækja skýrslu.'
    // Clear data on error
    summaryData.value = null
    waitTimeSeriesData.value = []
    lateSeriesData.value = []
    heatmapData.value = null
    growthData.value = []
    comparisonData.value = null
    forecastData.value = null
  } finally {
    isLoading.value = false
  }
}

watch(selectedReportType, () => {
  if (selectedReportType.value) {
    loadReportData()
  }
})

watch(selectedHeatmapType, () => {
  if (selectedReportType.value === 'heatmaps') {
    loadReportData()
  }
})

watch(
  () => [filters.value.fromDate, filters.value.toDate, filters.value.deliveryMethod, filters.value.restaurantId, filters.value.granularity],
  () => {
    if (selectedReportType.value) {
      loadReportData()
    }
  }
)

onMounted(async () => {
  await loadRestaurants()
})
</script>
