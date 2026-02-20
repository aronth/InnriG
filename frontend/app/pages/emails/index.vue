<template>
  <div class="h-[calc(100vh-4rem)] flex flex-col -my-6 -mx-4">
    <!-- Main Layout: Sidebar + Content + Workflow Sidebar -->
    <div class="flex-1 flex gap-0 overflow-hidden">
      <!-- Sidebar: Conversation List -->
      <div class="w-96 flex-shrink-0 bg-white border-r border-gray-200 flex flex-col overflow-hidden">
        <!-- Sidebar Header with Filters -->
        <div class="px-3 py-2 border-b border-gray-200 bg-gray-50 flex-shrink-0">
          <div class="flex items-center justify-between mb-2">
            <div class="text-sm font-semibold text-gray-700">
              Samtöl ({{ totalCount }})
            </div>
            <button
              @click="handleFilterChange"
              class="px-2 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              title="Endurnýja"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
            </button>
          </div>
          <div class="flex gap-2">
            <select
              v-model="filters.status"
              @change="handleFilterChange"
              class="flex-1 px-2 py-1 text-xs border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <option value="">Allar staður</option>
              <option value="New">Ný</option>
              <option value="InProgress">Í vinnslu</option>
              <option value="AwaitingResponse">Bíður svars</option>
              <option value="Resolved">Leyst</option>
              <option value="Archived">Geymt</option>
            </select>
            <select
              v-model="filters.classification"
              @change="handleFilterChange"
              class="flex-1 px-2 py-1 text-xs border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <option value="">Allar flokkanir</option>
              <option value="BuffetBooking">Buffet</option>
              <option value="TableBooking">Borðbókun</option>
              <option value="GroupBooking">Hópabókun</option>
              <option value="Complaint">Kvörtun</option>
              <option value="GeneralInquiry">Fyrirspurn</option>
              <option value="Other">Annað</option>
            </select>
          </div>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="flex-1 flex items-center justify-center">
          <div class="text-center">
            <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
            <p class="mt-2 text-sm text-gray-600">Hleður...</p>
          </div>
        </div>

        <!-- Conversation List with Virtual Scrolling -->
        <div 
          v-else 
          ref="scrollContainerRef"
          class="flex-1 overflow-y-auto"
          @scroll="handleScroll"
        >
          <!-- Virtual scroll container -->
          <div :style="{ height: `${totalHeight}px`, position: 'relative' }">
            <div :style="{ transform: `translateY(${offsetY}px)` }">
              <div
                v-for="conversation in visibleConversations"
                :key="conversation.id"
                :data-index="conversation._index"
                @click="selectConversation(conversation.id)"
                :class="[
                  'px-3 py-2 border-b border-gray-100 cursor-pointer transition-colors border-l-4',
                  selectedConversationId === conversation.id
                    ? 'bg-indigo-50'
                    : 'hover:bg-gray-50',
                  getStatusBorderColor(conversation.status)
                ]"
                :style="{ height: `${itemHeight}px` }"
              >
                <div class="flex items-start justify-between gap-2 mb-1">
                  <div class="flex-1 min-w-0">
                    <div class="text-sm font-semibold text-gray-900 truncate">
                      {{ conversation.fromName || conversation.fromEmail }}
                    </div>
                    <div class="text-xs text-gray-500 truncate">{{ conversation.fromEmail }}</div>
                  </div>
                  <div class="flex-shrink-0 text-right">
                    <div class="text-xs text-gray-500 whitespace-nowrap">
                      {{ formatShortDate24H(conversation.lastMessageReceivedAt) }}
                    </div>
                    <div v-if="conversation.messageCount > 1" class="text-xs text-gray-500 whitespace-nowrap">
                      {{ conversation.messageCount }} skilaboð
                    </div>
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <div class="text-sm text-gray-900 font-medium truncate flex-1 min-w-0">
                    {{ conversation.subject || '(Ekkert efni)' }}
                  </div>
                  <span
                    v-if="conversation.classification"
                    class="px-2 py-0.5 text-xs font-semibold rounded-full flex-shrink-0"
                    :class="getClassificationColor(conversation.classification)"
                  >
                    {{ getClassificationLabel(conversation.classification) }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Empty State -->
          <div v-if="conversations.length === 0 && !loading" class="text-center py-12 px-4">
            <p class="text-gray-500">Engin samtöl fundust</p>
          </div>

          <!-- Loading more indicator -->
          <div v-if="loadingMore" class="px-4 py-3 text-center">
            <div class="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-indigo-600"></div>
            <p class="text-xs text-gray-500 mt-1">Hleður fleiri...</p>
          </div>
        </div>
      </div>

      <!-- Main Content: Conversation View -->
      <div class="flex-1 bg-white flex flex-col overflow-hidden">
        <!-- Sticky Header -->
        <div
          v-if="selectedConversation"
          class="sticky top-0 z-10 bg-white border-b border-gray-200 px-4 py-2 flex-shrink-0"
        >
          <div class="flex items-start justify-between w-full gap-4">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <h2 class="text-lg font-bold text-gray-900 truncate">
                  {{ selectedConversation.subject || '(Ekkert efni)' }}
                </h2>
                <span class="text-sm text-gray-500 flex-shrink-0">
                  ({{ selectedConversation.messageCount }})
                </span>
              </div>
              <div class="flex items-center gap-3 flex-wrap">
                <div class="text-sm text-gray-700">
                  <span class="font-medium">Frá:</span>
                  <span class="ml-1">{{ selectedConversation.fromName || selectedConversation.fromEmail }}</span>
                  <span class="text-gray-500 ml-1">&lt;{{ selectedConversation.fromEmail }}&gt;</span>
                </div>
                <div class="text-sm text-gray-500">
                  {{ formatDate(selectedConversation.lastMessageReceivedAt) }}
                </div>
                <div class="flex items-center gap-2">
                  <span
                    v-if="selectedConversation.classification"
                    class="px-2 py-0.5 text-xs font-semibold rounded-full"
                    :class="getClassificationColor(selectedConversation.classification)"
                  >
                    {{ getClassificationLabel(selectedConversation.classification) }}
                  </span>
                  <span
                    class="px-2 py-0.5 text-xs font-semibold rounded-full"
                    :class="getStatusColor(selectedConversation.status)"
                  >
                    {{ getStatusLabel(selectedConversation.status) }}
                  </span>
                </div>
              </div>
            </div>
            <div class="flex items-center gap-2 flex-shrink-0">
              <button
                v-if="selectedConversation && !selectedConversation.messages?.some(m => m.isOutgoing)"
                @click="showReplyModal = true"
                class="px-3 py-1.5 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 flex items-center gap-1.5 text-sm"
                title="Svara tölvupósti"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" />
                </svg>
                Svara
              </button>
              <!-- Actions Dropdown -->
              <div class="relative" ref="actionsDropdownRef">
                <button
                  @click="showActionsDropdown = !showActionsDropdown"
                  class="p-1.5 text-gray-600 hover:bg-gray-100 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  title="Aðgerðir"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z" />
                  </svg>
                </button>
                <div
                  v-if="showActionsDropdown"
                  class="absolute right-0 mt-1 w-48 bg-white rounded-md shadow-lg border border-gray-200 z-20"
                >
                  <div class="py-1">
                    <button
                      @click="refreshConversation; showActionsDropdown = false"
                      :disabled="conversationLoading"
                      class="w-full px-4 py-2 text-sm text-left text-gray-700 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                    >
                      <svg
                        v-if="conversationLoading"
                        class="animate-spin h-4 w-4"
                        xmlns="http://www.w3.org/2000/svg"
                        fill="none"
                        viewBox="0 0 24 24"
                      >
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      <svg
                        v-else
                        class="h-4 w-4"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                      </svg>
                      Endurnýja
                    </button>
                    <button
                      @click="reparseConversation; showActionsDropdown = false"
                      :disabled="reparsing"
                      class="w-full px-4 py-2 text-sm text-left text-gray-700 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                    >
                      <svg
                        v-if="reparsing"
                        class="animate-spin h-4 w-4"
                        xmlns="http://www.w3.org/2000/svg"
                        fill="none"
                        viewBox="0 0 24 24"
                      >
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      <svg
                        v-else
                        class="h-4 w-4"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                      </svg>
                      {{ reparsing ? 'Endurflokkar...' : 'Endurflokka' }}
                    </button>
                    <div class="border-t border-gray-200 my-1"></div>
                    <button
                      @click="openJunkFilterModal(); showActionsDropdown = false"
                      class="w-full px-4 py-2 text-sm text-left text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                      Setja í ruslasíu
                    </button>
                    <div class="border-t border-gray-200 my-1"></div>
                    <div class="px-4 py-2">
                      <label class="block text-xs font-medium text-gray-700 mb-1">Staða</label>
                      <select
                        v-model="selectedStatus"
                        @change="updateStatus; showActionsDropdown = false"
                        class="w-full px-2 py-1 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                      >
                        <option value="New">Ný</option>
                        <option value="InProgress">Í vinnslu</option>
                        <option value="AwaitingResponse">Bíður svars</option>
                        <option value="Resolved">Leyst</option>
                        <option value="Archived">Geymt</option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Conversation Content -->
        <div v-if="selectedConversation && !conversationLoading" class="flex-1 overflow-y-auto">
          <!-- AI Extracted Data -->
          <div
            v-if="selectedConversation.extractedData"
            class="px-4 py-3 bg-gradient-to-br from-purple-50 via-indigo-50 to-blue-50 border-b-2 border-purple-300"
          >
            <!-- Collapsed View -->
            <div v-if="!aiSummaryExpanded" class="flex items-center justify-between gap-4">
              <div class="flex items-center gap-3 flex-wrap">
                <div class="flex items-center gap-2">
                  <div class="flex items-center gap-1.5 px-2 py-0.5 bg-gradient-to-r from-purple-600 to-indigo-600 text-white rounded-full text-xs font-semibold">
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                    </svg>
                    <span>AI</span>
                  </div>
                  <span
                    class="px-2 py-0.5 text-xs font-semibold rounded-full"
                    :class="getClassificationColor(selectedConversation.extractedData.classification)"
                  >
                    {{ getClassificationLabel(selectedConversation.extractedData.classification) }}
                  </span>
                </div>
                <div v-if="selectedConversation.extractedData.requestedDate" class="text-xs text-gray-700">
                  <span class="font-medium">Dagsetning:</span>
                  <span class="ml-1">{{ formatDateOnly(selectedConversation.extractedData.requestedDate) }}</span>
                </div>
                <div v-if="selectedConversation.extractedData.requestedTime" class="text-xs text-gray-700">
                  <span class="font-medium">Tími:</span>
                  <span class="ml-1">{{ formatTime(selectedConversation.extractedData.requestedTime) }}</span>
                </div>
                <div class="flex items-center gap-1.5">
                  <span class="text-xs text-gray-600">Áreiðanleiki:</span>
                  <div
                    class="px-2 py-0.5 rounded text-xs font-bold"
                    :class="getConfidenceColor(selectedConversation.extractedData.confidence)"
                  >
                    {{ (selectedConversation.extractedData.confidence * 100).toFixed(0) }}%
                  </div>
                </div>
              </div>
              <button
                @click="aiSummaryExpanded = true"
                class="p-1 text-gray-600 hover:text-gray-900 hover:bg-white/50 rounded transition-colors"
                title="Sýna allar upplýsingar"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </div>

            <!-- Expanded View -->
            <div v-else>
              <!-- AI Badge Header -->
              <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2">
                  <div class="flex items-center gap-2 px-3 py-1 bg-gradient-to-r from-purple-600 to-indigo-600 text-white rounded-full text-xs font-semibold">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                    </svg>
                    <span>Gervigreind</span>
                  </div>
                  <span class="text-sm font-semibold text-gray-800">Útdregin upplýsingar</span>
                </div>
                <div class="flex items-center gap-2">
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-gray-600">Áreiðanleiki:</span>
                    <div
                      class="px-2 py-1 rounded text-xs font-bold"
                      :class="getConfidenceColor(selectedConversation.extractedData.confidence)"
                    >
                      {{ (selectedConversation.extractedData.confidence * 100).toFixed(0) }}%
                    </div>
                  </div>
                  <button
                    @click="aiSummaryExpanded = false"
                    class="p-1 text-gray-600 hover:text-gray-900 hover:bg-white/50 rounded transition-colors"
                    title="Fela upplýsingar"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                    </svg>
                  </button>
                </div>
              </div>

            <!-- Extracted Fields -->
            <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              <!-- Classification -->
              <div class="col-span-2 md:col-span-3 lg:col-span-4">
                <div class="text-xs font-medium text-gray-600 mb-1">Flokkun</div>
                <div class="text-sm font-semibold text-gray-900">
                  {{ getClassificationLabel(selectedConversation.extractedData.classification) }}
                </div>
              </div>

              <!-- Date & Time -->
              <div v-if="selectedConversation.extractedData.requestedDate">
                <div class="text-xs font-medium text-gray-600 mb-1">Dagsetning</div>
                <div class="text-sm font-semibold text-gray-900">{{ formatDateOnly(selectedConversation.extractedData.requestedDate) }}</div>
              </div>
              <div v-if="selectedConversation.extractedData.requestedTime">
                <div class="text-xs font-medium text-gray-600 mb-1">Tími</div>
                <div class="text-sm font-semibold text-gray-900">{{ formatTime(selectedConversation.extractedData.requestedTime) }}</div>
              </div>

              <!-- Guest Counts -->
              <div v-if="selectedConversation.extractedData.guestCount">
                <div class="text-xs font-medium text-gray-600 mb-1">Fjöldi gesta</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.guestCount }}</div>
              </div>
              <div v-if="selectedConversation.extractedData.adultCount">
                <div class="text-xs font-medium text-gray-600 mb-1">Fullorðnir</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.adultCount }}</div>
              </div>
              <div v-if="selectedConversation.extractedData.childCount">
                <div class="text-xs font-medium text-gray-600 mb-1">Börn</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.childCount }}</div>
              </div>

              <!-- Location -->
              <div v-if="selectedConversation.extractedData.locationCode">
                <div class="text-xs font-medium text-gray-600 mb-1">Staðsetning</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.locationCode }}</div>
              </div>

              <!-- Contact Information -->
              <div v-if="selectedConversation.extractedData.contactName">
                <div class="text-xs font-medium text-gray-600 mb-1">Nafn</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.contactName }}</div>
              </div>
              <div v-if="selectedConversation.extractedData.contactPhone">
                <div class="text-xs font-medium text-gray-600 mb-1">Sími</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.contactPhone }}</div>
              </div>
              <div v-if="selectedConversation.extractedData.contactEmail">
                <div class="text-xs font-medium text-gray-600 mb-1">Netfang</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.contactEmail }}</div>
              </div>

              <!-- Special Requests -->
              <div v-if="selectedConversation.extractedData.specialRequests" class="col-span-2 md:col-span-3 lg:col-span-4">
                <div class="text-xs font-medium text-gray-600 mb-1">Sérstakar beiðnir</div>
                <div class="text-sm font-semibold text-gray-900">{{ selectedConversation.extractedData.specialRequests }}</div>
              </div>
            </div>

              <!-- Footer with extraction timestamp -->
              <div class="mt-4 pt-3 border-t border-purple-200">
                <div class="text-xs text-gray-500">
                  Útdregið: {{ formatDateTime(selectedConversation.extractedData.extractedAt) }}
                </div>
              </div>
            </div>
          </div>

          <!-- Bookings for Date (only for booking-related classifications) -->
          <div
            v-if="selectedConversation.extractedData?.requestedDate && isBookingClassification(selectedConversation.classification)"
            class="px-4 py-4 bg-gradient-to-br from-green-50 to-emerald-50 border-b-2 border-green-300"
          >
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-2">
                <svg class="w-5 h-5 text-green-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <span class="text-sm font-semibold text-gray-800">
                  Bókanir fyrir {{ formatDate(selectedConversation.extractedData.requestedDate) }}
                </span>
              </div>
              <div class="flex items-center gap-2">
                <button
                  v-if="!bookingsLoading"
                  @click="loadBookingsForDate(selectedConversation.extractedData!.requestedDate!)"
                  class="text-xs text-green-700 hover:text-green-900 font-medium"
                >
                  Endurnýja
                </button>
                <a
                  :href="getBookingListingUrl(selectedConversation.extractedData!.requestedDate!)"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="text-xs px-2 py-1 bg-indigo-600 text-white rounded hover:bg-indigo-700 font-medium flex items-center gap-1"
                >
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                  Opna Greifinn.is
                </a>
              </div>
            </div>

            <!-- Loading State -->
            <div v-if="bookingsLoading" class="text-center py-4">
              <div class="inline-block animate-spin rounded-full h-5 w-5 border-b-2 border-green-600"></div>
              <p class="mt-2 text-xs text-gray-600">Hleður bókunum...</p>
            </div>

            <!-- Bookings List -->
            <div v-else-if="bookingsForDate.length > 0" class="overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                  <tr>
                    <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Tími</th>
                    <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Nafn</th>
                    <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Gestir</th>
                  </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                  <tr
                    v-for="booking in bookingsForDate"
                    :key="booking.bookingId || booking.detailUrl"
                    class="hover:bg-gray-50"
                  >
                    <td class="px-3 py-2 whitespace-nowrap text-sm text-gray-900">
                      {{ booking.startTime }}
                    </td>
                    <td class="px-3 py-2 text-sm text-gray-900">
                      <div class="flex items-center gap-2">
                        <span>{{ booking.contactName }}</span>
                        <a
                          v-if="booking.detailUrl"
                          :href="booking.detailUrl"
                          target="_blank"
                          rel="noopener noreferrer"
                          class="text-indigo-600 hover:text-indigo-800"
                          title="Opna bókun"
                        >
                          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                          </svg>
                        </a>
                      </div>
                    </td>
                    <td class="px-3 py-2 whitespace-nowrap text-sm text-gray-900">
                      {{ booking.guestCount || 0 }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-4">
              <p class="text-sm text-gray-600">Engar bókanir fundust fyrir þessa dagsetningu</p>
            </div>
          </div>

          <!-- Messages -->
          <div>
            <div v-if="selectedConversation.messages && selectedConversation.messages.length > 0" class="border-t border-gray-200">
              <div
                v-for="message in selectedConversation.messages"
                :key="message.id"
                :class="[
                  'border-b border-gray-200 transition-colors',
                  message.isAIResponse 
                    ? 'bg-blue-50 hover:bg-blue-100 border-l-4 border-l-blue-500' 
                    : 'hover:bg-gray-50'
                ]"
              >
                <!-- AI Processing Status - Top -->
                <div v-if="message.classificationQueueStatus" class="px-4 py-1.5 bg-gray-100 border-b border-gray-200">
                  <div class="flex items-center gap-2">
                    <div
                      class="flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium"
                      :class="getQueueStatusColor(message.classificationQueueStatus)"
                    >
                      <svg
                        v-if="message.classificationQueueStatus === 'Processing'"
                        class="animate-spin h-3 w-3"
                        xmlns="http://www.w3.org/2000/svg"
                        fill="none"
                        viewBox="0 0 24 24"
                      >
                        <circle
                          class="opacity-25"
                          cx="12"
                          cy="12"
                          r="10"
                          stroke="currentColor"
                          stroke-width="4"
                        ></circle>
                        <path
                          class="opacity-75"
                          fill="currentColor"
                          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                        ></path>
                      </svg>
                      <svg
                        v-else-if="message.classificationQueueStatus === 'Completed'"
                        class="h-3 w-3"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                      </svg>
                      <svg
                        v-else-if="message.classificationQueueStatus === 'Failed'"
                        class="h-3 w-3"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                      <svg
                        v-else
                        class="h-3 w-3"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      <span>{{ getQueueStatusLabel(message.classificationQueueStatus) }}</span>
                    </div>
                    <span v-if="message.classificationQueuedAt" class="text-xs text-gray-500">
                      Í biðröð: {{ formatShortDate(message.classificationQueuedAt) }}
                    </span>
                    <span v-if="message.classificationCompletedAt" class="text-xs text-gray-500">
                      Lokið: {{ formatShortDate(message.classificationCompletedAt) }}
                    </span>
                  </div>
                </div>
                
                <!-- Slim Header -->
                <div :class="['px-4 py-2 border-b border-gray-200', message.isAIResponse ? 'bg-blue-100' : 'bg-gray-50']">
                  <div class="flex items-center justify-between gap-4 text-xs">
                    <div class="flex items-center gap-3 flex-1 min-w-0">
                      <div class="flex-shrink-0">
                        <span class="font-medium text-gray-700">Frá:</span>
                        <span class="text-gray-900 ml-1">{{ message.fromName || message.fromEmail }}</span>
                      </div>
                      <div v-if="!message.isAIResponse" class="flex-shrink-0">
                        <span class="font-medium text-gray-700">Til:</span>
                        <span class="text-gray-900 ml-1">{{ message.toName || message.toEmail }}</span>
                      </div>
                      <div class="flex-1 min-w-0">
                        <span class="font-medium text-gray-700">Efni:</span>
                        <span class="text-gray-900 ml-1 truncate">{{ message.subject }}</span>
                      </div>
                      <div v-if="message.isAIResponse" class="flex-shrink-0">
                        <span class="px-2 py-0.5 bg-blue-600 text-white text-xs font-semibold rounded">
                          AI Greining
                        </span>
                      </div>
                    </div>
                    <div class="flex-shrink-0 text-gray-600 whitespace-nowrap">
                      {{ formatDateTime(message.receivedDateTime) }}
                    </div>
                  </div>
                </div>
                
                <!-- AI Message Body Toggle -->
                <div v-if="message.isAIResponse" class="px-4 py-2">
                  <button
                    @click="toggleAIMessage(message.id)"
                    class="text-indigo-600 hover:text-indigo-800 text-sm font-medium flex items-center gap-1"
                  >
                    <svg 
                      class="w-4 h-4 transition-transform"
                      :class="{ 'rotate-180': expandedAIMessages[message.id] }"
                      fill="none" 
                      stroke="currentColor" 
                      viewBox="0 0 24 24"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                    {{ expandedAIMessages[message.id] ? 'Fela' : 'Sýna' }} AI greiningu
                  </button>
                </div>
                
                <!-- AI Message Body -->
                <div v-if="message.isAIResponse && message.messageBody && expandedAIMessages[message.id]" class="px-4 pb-4">
                  <div class="bg-white p-4 rounded border border-blue-200 whitespace-pre-wrap text-sm text-gray-800 font-mono">
                    {{ message.messageBody }}
                  </div>
                </div>
                
                <!-- Regular Message Body Toggle (incoming/outgoing) -->
                <div v-else-if="!message.isAIResponse" class="px-4 py-2">
                  <button
                    @click="loadMessageBody(message.id)"
                    class="text-indigo-600 hover:text-indigo-800 text-sm font-medium"
                  >
                    {{ messageBodies[message.id] ? 'Fela' : 'Sýna' }} efni
                  </button>
                </div>
                
                <!-- Regular Message Content -->
                <div
                  v-if="messageBodies[message.id] && !message.isAIResponse"
                  class="px-4 pb-4"
                >
                  <div
                    class="prose prose-sm max-w-none bg-white p-4 rounded border border-gray-200"
                    v-html="processMessageHtml(messageBodies[message.id]?.html || messageBodies[message.id]?.text)"
                  ></div>
                </div>
              </div>
            </div>
            <div v-else class="px-4 py-8 text-center text-gray-500 border-t border-gray-200">
              <p class="text-sm">Engin skilaboð í þessu samtal</p>
            </div>
          </div>
        </div>

        <!-- Loading Conversation -->
        <div v-if="conversationLoading" class="flex-1 flex items-center justify-center">
          <div class="text-center">
            <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
            <p class="mt-2 text-gray-600">Hleður samtali...</p>
          </div>
        </div>

        <!-- Empty State -->
        <div v-if="!selectedConversation && !loading" class="flex-1 flex items-center justify-center">
          <div class="text-center text-gray-500">
            <svg
              class="mx-auto h-12 w-12 text-gray-400 mb-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
              />
            </svg>
            <p class="text-lg font-medium">Veldu samtal til að skoða</p>
            <p class="text-sm mt-1">Smelltu á samtal í listanum til vinstri</p>
          </div>
        </div>
      </div>

      <!-- Right Sidebar: Workflow -->
      <WorkflowSidebar
        v-if="selectedConversationId"
        :conversation-id="selectedConversationId"
        @reply-click="handleWorkflowReplyClick"
      />
    </div>

    <!-- Reply Modal -->
    <EmailReplyModal
      ref="replyModalRef"
      v-model:show="showReplyModal"
      :to-email="selectedConversation?.fromEmail || ''"
      :to-name="selectedConversation?.fromName || ''"
      :subject="replySubject"
      :draft-body="draftReplyBody"
      :email-mappings="emailMappings"
      :email-signature="emailSignature"
      @send="handleReplySend"
      @cancel="handleReplyCancel"
    />

    <!-- Junk Filter Modal -->
    <div
      v-if="showJunkFilterModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="closeJunkFilterModal"
    >
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md m-4">
        <div class="px-6 py-4 border-b border-gray-200">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold text-gray-900">Setja í ruslasíu</h3>
            <button
              @click="closeJunkFilterModal"
              class="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
        <div class="px-6 py-4 space-y-4">
          <!-- Subject Field -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Efnisgrein</label>
            <div class="relative">
              <input
                v-model="junkFilterForm.subject"
                type="text"
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-8"
                placeholder="Efnisgrein (valfrjálst)"
              />
              <button
                v-if="junkFilterForm.subject"
                @click="junkFilterForm.subject = ''"
                class="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          <!-- From Email Field -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Netfang sendanda</label>
            <div class="relative">
              <input
                v-model="junkFilterForm.senderEmail"
                type="text"
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-8"
                placeholder="Netfang sendanda (valfrjálst)"
              />
              <button
                v-if="junkFilterForm.senderEmail"
                @click="junkFilterForm.senderEmail = ''"
                class="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          <p class="text-xs text-gray-500">
            Verður að skilgreina annaðhvort efnisgrein eða netfang sendanda
          </p>

          <div v-if="junkFilterError" class="text-sm text-red-600 bg-red-50 p-2 rounded">
            {{ junkFilterError }}
          </div>
        </div>
        <div class="px-6 py-4 border-t border-gray-200 flex justify-end gap-3">
          <button
            @click="closeJunkFilterModal"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            Hætta við
          </button>
          <button
            @click="saveJunkFilter"
            :disabled="savingJunkFilter || (!junkFilterForm.subject?.trim() && !junkFilterForm.senderEmail?.trim())"
            class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            <svg
              v-if="savingJunkFilter"
              class="animate-spin h-4 w-4"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Vista
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EmailConversationDto, EmailMessageBodyDto } from '~/types/email'
import type { BookingWeekDto, BookingDayDto, BookingDto } from '~/types/booking'
import type { UserEmailMappingDto } from '~/types/userEmailSettings'

const route = useRoute()
const { currentUser } = useAuth()
const { getConversations, getConversation, getMessageBody, updateStatus: updateStatusApi, reparseConversation: reparseConversationApi, replyToConversation } = useEmails()
const { getTableBookings } = useTableBookings()
const { getEmailMappings, getEmailSignature } = useUserEmailSettings()
const { createJunkFilter } = useEmailJunkFilters()

const conversations = ref<EmailConversationDto[]>([])
const loading = ref(true)
const loadingMore = ref(false)
const hasMore = ref(true)
const pageSize = ref(50)
const totalCount = ref(0)

// Virtual scrolling
const scrollContainerRef = ref<HTMLElement | null>(null)
const itemHeight = 80 // Estimated height of each conversation item in pixels
const visibleBuffer = 5 // Number of items to render outside viewport
const scrollTop = ref(0)
const containerHeight = ref(0)

const selectedConversationId = ref<string | null>(null)
const selectedConversation = ref<EmailConversationDto | null>(null)
const conversationLoading = ref(false)
const messageBodies = ref<Record<string, EmailMessageBodyDto>>({})
const expandedAIMessages = ref<Record<string, boolean>>({})
const selectedStatus = ref('')
const reparsing = ref(false)

const bookingsForDate = ref<any[]>([])
const bookingsLoading = ref(false)

const statusMessage = ref<string>('')
const statusMessageType = ref<'loading' | 'success' | 'error' | 'info'>('info')

const showReplyModal = ref(false)
const showActionsDropdown = ref(false)
const actionsDropdownRef = ref<HTMLElement | null>(null)
const aiSummaryExpanded = ref(false)
const showJunkFilterModal = ref(false)
const savingJunkFilter = ref(false)
const junkFilterError = ref('')
const junkFilterForm = ref({
  subject: '',
  senderEmail: ''
})
const emailMappings = ref<UserEmailMappingDto[]>([])
const emailSignature = ref<string>('')
const replyModalRef = ref<InstanceType<typeof EmailReplyModal> | null>(null)

const filters = ref({
  status: '',
  classification: ''
})

const setStatusMessage = (message: string, type: 'loading' | 'success' | 'error' | 'info' = 'info', duration?: number) => {
  statusMessage.value = message
  statusMessageType.value = type
  if (duration && type !== 'loading') {
    setTimeout(() => {
      if (statusMessage.value === message) {
        clearStatusMessage()
      }
    }, duration)
  }
}

const clearStatusMessage = () => {
  statusMessage.value = ''
}

const handleFilterChange = () => {
  conversations.value = []
  if (scrollContainerRef.value) {
    scrollContainerRef.value.scrollTop = 0
  }
  loadConversations(false)
}

const loadConversations = async (append = false) => {
  if (append) {
    loadingMore.value = true
  } else {
    loading.value = true
    conversations.value = []
    hasMore.value = true
    setStatusMessage('Hleður samtölum...', 'loading')
  }
  
  try {
    const currentPage = Math.floor(conversations.value.length / pageSize.value) + 1
    const result = await getConversations({
      status: filters.value.status || undefined,
      classification: filters.value.classification || undefined,
      page: currentPage,
      pageSize: pageSize.value
    })
    
    if (append) {
      conversations.value = [...conversations.value, ...result.items]
    } else {
      conversations.value = result.items
    }
    
    totalCount.value = result.totalCount
    hasMore.value = conversations.value.length < result.totalCount
    
    if (!append) {
      setStatusMessage(`Sótt ${result.items.length} samtöl`, 'success', 2000)
    }

    // If we have a selected conversation, try to find it in the new list
    if (selectedConversationId.value) {
      const found = conversations.value.find(c => c.id === selectedConversationId.value)
      if (found) {
        // Update the selected conversation with fresh data
        if (selectedConversation.value) {
          selectedConversation.value.status = found.status
          selectedConversation.value.classification = found.classification
          selectedConversation.value.messageCount = found.messageCount
          selectedConversation.value.lastMessageReceivedAt = found.lastMessageReceivedAt
        }
      } else if (!append) {
        // Conversation not in current list, reload it
        await loadSelectedConversation(selectedConversationId.value)
      }
    }
  } catch (error) {
    console.error('Error loading conversations:', error)
    if (!append) {
      setStatusMessage('Villa kom upp við að hlaða samtölum', 'error', 5000)
    }
  } finally {
    loading.value = false
    loadingMore.value = false
  }
}

const sortAndExpandMessages = (conversation: EmailConversationDto) => {
  // Sort messages by date descending (newest first)
  if (conversation.messages && conversation.messages.length > 0) {
    conversation.messages.sort((a, b) => {
      const dateA = new Date(a.receivedDateTime).getTime()
      const dateB = new Date(b.receivedDateTime).getTime()
      return dateB - dateA // Descending order (newest first)
    })
  }
}

const loadBookingsForDate = async (dateString: string, silent = false) => {
  if (!dateString) return

  bookingsLoading.value = true
  if (!silent) {
    bookingsForDate.value = []
  }
  if (!silent) {
    setStatusMessage('Hleður bókunum...', 'loading')
  }

  try {
    // Parse the date string
    const date = new Date(dateString)
    const fromDate = new Date(date)
    fromDate.setHours(0, 0, 0, 0)
    
    // The filter is midnight to midnight, so toDate should be the next day
    const toDate = new Date(date)
    toDate.setDate(toDate.getDate() + 1)
    toDate.setHours(0, 0, 0, 0)

    // Fetch bookings for the specific date using tableBookingService
    const result = await getTableBookings(
      fromDate,
      toDate,
      undefined, // contactName
      undefined, // contactPhone
      undefined, // statusId
      1, // page
      1000 // pageSize - large to get all bookings for the day
    )

    // Map TableBookingDto to display format
    bookingsForDate.value = result.bookings.map(booking => {
      // Format time in 24-hour format (HH:mm) like greifinn.is
      let startTime = ''
      if (booking.timestamp) {
        const date = new Date(booking.timestamp)
        const hours = String(date.getHours()).padStart(2, '0')
        const minutes = String(date.getMinutes()).padStart(2, '0')
        startTime = `${hours}:${minutes}`
      }
      
      return {
        bookingId: booking.bookingId,
        timestamp: booking.timestamp,
        contactName: booking.contactName || 'Óþekkt',
        contactPhone: booking.contactPhone,
        guestCount: booking.guestCount || 0,
        status: booking.status || 'Óþekkt',
        hasComment: booking.hasComment,
        detailUrl: booking.detailUrl,
        startTime: startTime,
        date: dateString
      }
    })
    
    if (!silent) {
      if (bookingsForDate.value.length > 0) {
        setStatusMessage(`Fundust ${bookingsForDate.value.length} bókanir`, 'success', 2000)
      } else {
        setStatusMessage('Engar bókanir fundust fyrir þessa dagsetningu', 'info', 3000)
      }
    }
  } catch (error) {
    console.error('Error loading bookings:', error)
    if (!silent) {
      bookingsForDate.value = []
      setStatusMessage('Villa kom upp við að hlaða bókunum', 'error', 5000)
    }
  } finally {
    bookingsLoading.value = false
  }
}

const loadSelectedConversation = async (id: string, silent = false) => {
  // Only show loading state if not silent (to avoid hiding conversation during polling)
  if (!silent) {
    conversationLoading.value = true
    messageBodies.value = {} // Clear previous message bodies
    expandedAIMessages.value = {} // Clear expanded AI messages
    bookingsForDate.value = [] // Clear previous bookings
    setStatusMessage('Hleður samtali...', 'loading')
  }
  try {
    const conversation = await getConversation(id)
    
    // Sort messages by date descending (newest first)
    sortAndExpandMessages(conversation)
    
    // Auto-expand only the latest incoming message and latest AI message
    if (conversation.messages && conversation.messages.length > 0) {
      // Find the latest incoming message (not outgoing, not AI)
      const latestIncomingMessage = conversation.messages.find(m => !m.isOutgoing && !m.isAIResponse)
      
      // Find the latest AI message
      const latestAIMessage = conversation.messages.find(m => m.isAIResponse)
      
      // Load body for latest incoming message
      if (latestIncomingMessage && !messageBodies.value[latestIncomingMessage.id]) {
        try {
          if (!silent) {
            setStatusMessage('Hleður skilaboði...', 'loading')
          }
          const body = await getMessageBody(latestIncomingMessage.id)
          messageBodies.value[latestIncomingMessage.id] = body
        } catch (error) {
          console.error('Error loading latest incoming message body:', error)
        }
      }
      
      // Expand latest AI message
      if (latestAIMessage) {
        expandedAIMessages.value[latestAIMessage.id] = true
      }
    }
    
    // Update conversation data (this preserves existing message bodies if silent)
    selectedConversation.value = conversation
    selectedConversationId.value = id
    selectedStatus.value = conversation.status
    
    // Auto-expand new messages that weren't there before
    if (conversation.messages && conversation.messages.length > 0) {
      // Find the latest incoming message (not outgoing, not AI)
      const latestIncomingMessage = conversation.messages.find(m => !m.isOutgoing && !m.isAIResponse)
      
      // Find the latest AI message
      const latestAIMessage = conversation.messages.find(m => m.isAIResponse)
      
      // Load body for latest incoming message if not already loaded
      if (latestIncomingMessage && !messageBodies.value[latestIncomingMessage.id]) {
        try {
          const body = await getMessageBody(latestIncomingMessage.id)
          messageBodies.value[latestIncomingMessage.id] = body
        } catch (error) {
          console.error('Error loading latest incoming message body:', error)
        }
      }
      
      // Expand latest AI message if not already expanded
      if (latestAIMessage && !expandedAIMessages.value[latestAIMessage.id]) {
        expandedAIMessages.value[latestAIMessage.id] = true
      }
    }

    // Load bookings if a date is available and classification is booking-related (only if not silent or not already loaded)
    if (conversation.extractedData?.requestedDate && 
        isBookingClassification(conversation.classification) &&
        (!silent || bookingsForDate.value.length === 0)) {
      await loadBookingsForDate(conversation.extractedData.requestedDate, silent)
    } else if (!silent) {
      setStatusMessage('Samtali hlaðið', 'success', 2000)
    }

    // Update URL without navigation
    await navigateTo(`/emails?id=${id}`, { replace: true, external: false })
  } catch (error) {
    console.error('Error loading conversation:', error)
    if (!silent) {
      setStatusMessage('Villa kom upp við að hlaða samtali', 'error', 5000)
    }
  } finally {
    if (!silent) {
      conversationLoading.value = false
    }
  }
}

const selectConversation = (id: string) => {
  if (selectedConversationId.value === id) return
  selectedConversationId.value = id
  showActionsDropdown.value = false // Close dropdown when conversation changes
  aiSummaryExpanded.value = false // Collapse AI summary when conversation changes
  loadSelectedConversation(id)
}

// Virtual scrolling computed properties
const totalHeight = computed(() => conversations.value.length * itemHeight)

const visibleRange = computed(() => {
  if (!containerHeight.value || conversations.value.length === 0) {
    return { start: 0, end: Math.min(visibleBuffer * 2, conversations.value.length) }
  }
  
  const start = Math.max(0, Math.floor(scrollTop.value / itemHeight) - visibleBuffer)
  const visibleCount = Math.ceil(containerHeight.value / itemHeight)
  const end = Math.min(conversations.value.length, start + visibleCount + visibleBuffer * 2)
  
  return { start, end }
})

const visibleConversations = computed(() => {
  return conversations.value
    .slice(visibleRange.value.start, visibleRange.value.end)
    .map((conv, idx) => ({
      ...conv,
      _index: visibleRange.value.start + idx
    }))
})

const offsetY = computed(() => visibleRange.value.start * itemHeight)

// Scroll handler for infinite scroll
const handleScroll = (event: Event) => {
  const target = event.target as HTMLElement
  scrollTop.value = target.scrollTop
  containerHeight.value = target.clientHeight
  
  // Check if we're near the bottom (within 200px)
  const scrollBottom = target.scrollHeight - target.scrollTop - target.clientHeight
  if (scrollBottom < 200 && hasMore.value && !loadingMore.value && !loading.value) {
    loadConversations(true)
  }
}

// Update container height on resize
const updateContainerHeight = () => {
  if (scrollContainerRef.value) {
    containerHeight.value = scrollContainerRef.value.clientHeight
  }
}

const loadMessageBody = async (messageId: string) => {
  if (messageBodies.value[messageId]) {
    delete messageBodies.value[messageId]
    return
  }

  try {
    const body = await getMessageBody(messageId)
    messageBodies.value[messageId] = body
  } catch (error) {
    console.error('Error loading message body:', error)
  }
}

const toggleAIMessage = (messageId: string) => {
  expandedAIMessages.value[messageId] = !expandedAIMessages.value[messageId]
}

const updateStatus = async () => {
  if (!selectedConversation.value) return

  setStatusMessage('Uppfæri staðu...', 'loading')
  try {
    await updateStatusApi(selectedConversation.value.id, selectedStatus.value)
    if (selectedConversation.value) {
      selectedConversation.value.status = selectedStatus.value
    }
    // Update in list as well
    const index = conversations.value.findIndex(c => c.id === selectedConversation.value?.id)
    if (index !== -1 && conversations.value[index]) {
      conversations.value[index].status = selectedStatus.value
    }
    setStatusMessage('Staða uppfærð', 'success', 2000)
  } catch (error) {
    console.error('Error updating status:', error)
    setStatusMessage('Villa kom upp við að uppfæra staðu', 'error', 5000)
  }
}

const refreshConversation = async () => {
  if (!selectedConversation.value) return

  setStatusMessage('Endurnýjar samtal...', 'loading')
  await loadSelectedConversation(selectedConversation.value.id)
  // Also refresh the conversation list to get updated counts/status
  await loadConversations()
}

const reparseConversation = async () => {
  if (!selectedConversation.value) return

  reparsing.value = true
  setStatusMessage('Endurflokkar samtal...', 'loading')
  try {
    await reparseConversationApi(selectedConversation.value.id)
    setStatusMessage('Endurflokkun í vinnslu, bíður á niðurstöðu...', 'info')
    // Reload conversation after a short delay to allow classification to process
    setTimeout(() => {
      if (selectedConversation.value) {
        setStatusMessage('Hleður uppfærðum gögnum...', 'loading')
        loadSelectedConversation(selectedConversation.value.id)
        loadConversations()
        setStatusMessage('Endurflokkun lokið', 'success', 3000)
      }
    }, 2000)
  } catch (error) {
    console.error('Error re-parsing conversation:', error)
    setStatusMessage('Villa kom upp við endurflokkun', 'error', 5000)
  } finally {
    reparsing.value = false
  }
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

const formatShortDate = (dateString: string) => {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) {
    return new Intl.DateTimeFormat('is-IS', {
      hour: '2-digit',
      minute: '2-digit'
    }).format(date)
  } else if (diffDays === 1) {
    return 'Í gær'
  } else if (diffDays < 7) {
    return new Intl.DateTimeFormat('is-IS', {
      weekday: 'short'
    }).format(date)
  } else {
    return new Intl.DateTimeFormat('is-IS', {
      month: 'short',
      day: 'numeric'
    }).format(date)
  }
}

const formatShortDate24H = (dateString: string) => {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) {
    // Today: show 24-hour time
    return new Intl.DateTimeFormat('is-IS', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    }).format(date)
  } else if (diffDays === 1) {
    return 'Í gær'
  } else if (diffDays < 7) {
    return new Intl.DateTimeFormat('is-IS', {
      weekday: 'short'
    }).format(date)
  } else {
    return new Intl.DateTimeFormat('is-IS', {
      month: 'short',
      day: 'numeric'
    }).format(date)
  }
}

const formatDateTime = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

const formatTime = (timeString: string) => {
  const time = timeString.split(':')
  return `${time[0]}:${time[1]}`
}

const getBookingListingUrl = (dateString: string): string => {
  const date = new Date(dateString)
  const fromDate = new Date(date)
  fromDate.setHours(0, 0, 0, 0)
  
  // The filter is midnight to midnight, so toDate should be the next day
  const toDate = new Date(date)
  toDate.setDate(toDate.getDate() + 1)
  toDate.setHours(0, 0, 0, 0)
  
  // Format dates in Icelandic format (dd.MM.yyyy)
  const formatIcelandicDate = (d: Date): string => {
    const day = String(d.getDate()).padStart(2, '0')
    const month = String(d.getMonth() + 1).padStart(2, '0')
    const year = d.getFullYear()
    return `${day}.${month}.${year}`
  }
  
  const baseUrl = 'https://www.greifinn.is/is/bordapontun/booking'
  const params = new URLSearchParams({
    'flt-timestamp[from]': formatIcelandicDate(fromDate),
    'flt-timestamp[to]': formatIcelandicDate(toDate),
    'flt-contactName': '',
    'flt-contactPhone': '',
    'flt-status': '-1',
    'itemCount': '-1'
  })
  
  return `${baseUrl}?${params.toString()}`
}

const formatDateOnly = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date)
}

const getStatusLabel = (status: string) => {
  const labels: Record<string, string> = {
    New: 'Ný',
    InProgress: 'Í vinnslu',
    AwaitingResponse: 'Bíður svars',
    Resolved: 'Leyst',
    Archived: 'Geymt'
  }
  return labels[status] || status
}

const getStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    New: 'bg-blue-100 text-blue-800',
    InProgress: 'bg-yellow-100 text-yellow-800',
    AwaitingResponse: 'bg-orange-100 text-orange-800',
    Resolved: 'bg-green-100 text-green-800',
    Archived: 'bg-gray-100 text-gray-800'
  }
  return colors[status] || 'bg-gray-100 text-gray-800'
}

const getStatusBorderColor = (status: string) => {
  const colors: Record<string, string> = {
    New: 'border-l-blue-500',
    InProgress: 'border-l-red-500',
    AwaitingResponse: 'border-l-orange-500',
    Resolved: 'border-l-green-500',
    Archived: 'border-l-gray-400'
  }
  return colors[status] || 'border-l-gray-400'
}

const getClassificationLabel = (classification: string) => {
  const labels: Record<string, string> = {
    BuffetBooking: 'Buffet',
    TableBooking: 'Borðbókun',
    GroupBooking: 'Hópabókun',
    Complaint: 'Kvörtun',
    GeneralInquiry: 'Fyrirspurn',
    Other: 'Annað'
  }
  return labels[classification] || classification
}

const getClassificationColor = (classification: string) => {
  const colors: Record<string, string> = {
    BuffetBooking: 'bg-purple-100 text-purple-800',
    TableBooking: 'bg-indigo-100 text-indigo-800',
    GroupBooking: 'bg-blue-100 text-blue-800',
    Complaint: 'bg-red-100 text-red-800',
    GeneralInquiry: 'bg-gray-100 text-gray-800',
    Other: 'bg-gray-100 text-gray-800'
  }
  return colors[classification] || 'bg-gray-100 text-gray-800'
}

const isBookingClassification = (classification: string | null | undefined) => {
  if (!classification) return false
  return classification === 'TableBooking' || 
         classification === 'BuffetBooking' || 
         classification === 'GroupBooking'
}

const getConfidenceColor = (confidence: number) => {
  if (confidence >= 0.8) {
    return 'bg-green-100 text-green-800'
  } else if (confidence >= 0.6) {
    return 'bg-yellow-100 text-yellow-800'
  } else {
    return 'bg-orange-100 text-orange-800'
  }
}

const getBookingStatusColor = (status: string) => {
  const statusLower = status.toLowerCase()
  if (statusLower.includes('confirmed') || statusLower.includes('staðfest')) {
    return 'bg-green-100 text-green-800'
  } else if (statusLower.includes('pending') || statusLower.includes('bið')) {
    return 'bg-yellow-100 text-yellow-800'
  } else if (statusLower.includes('cancelled') || statusLower.includes('hætt')) {
    return 'bg-red-100 text-red-800'
  } else {
    return 'bg-gray-100 text-gray-800'
  }
}

const getQueueStatusLabel = (status: string) => {
  const labels: Record<string, string> = {
    Pending: 'Í biðröð',
    Processing: 'Í vinnslu',
    Completed: 'Lokið',
    Failed: 'Mistókst'
  }
  return labels[status] || status
}

const getQueueStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Processing: 'bg-blue-100 text-blue-800',
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800'
  }
  return colors[status] || 'bg-gray-100 text-gray-800'
}

const processMessageHtml = (html: string | undefined): string => {
  if (!html) return ''
  
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  
  // Replace relative attachment URLs with absolute URLs
  // This handles URLs like /api/emails/messages/.../attachments/...
  let processed = html.replace(
    /src=["'](\/api\/emails\/messages\/[^"']+\/attachments\/[^"']+)["']/g,
    `src="${apiBase}$1"`
  )
  
  // Remove or hide images with CID URLs that weren't replaced by the backend
  // Instead of replacing with a transparent pixel, hide the image entirely
  // to prevent stretched placeholder images
  
  // Handle <img> tags with CID URLs in src - hide them by adding display:none
  processed = processed.replace(
    /<img([^>]*)\s+src=["']cid:[^"']*["']([^>]*)>/gi,
    (match, before, after) => {
      // Check if style attribute already exists
      if (before.includes('style=') || after.includes('style=')) {
        // Add display:none to existing style
        return match.replace(/style=["']([^"']*)["']/i, (styleMatch, styleContent) => {
          return `style="${styleContent}; display: none !important;"`
        }).replace(/src=["']cid:[^"']*["']/gi, '')
      } else {
        // Add new style attribute
        return `<img${before}${after} style="display: none !important;">`
      }
    }
  )
  
  // Handle CID URLs in src attributes (standalone, not in img tags)
  processed = processed.replace(
    /src=["']cid:[^"']*["']/gi,
    'src=""'
  )
  
  // Handle CID URLs in src attributes (without quotes, edge case)
  processed = processed.replace(
    /src=cid:[^\s>]*/gi,
    'src=""'
  )
  
  // Handle CID URLs in href attributes
  processed = processed.replace(
    /href=["']cid:[^"']*["']/gi,
    'href="#"'
  )
  
  // Handle CID URLs in style attributes (background-image, etc.)
  processed = processed.replace(
    /url\(["']?cid:[^"')]*["']?\)/gi,
    'url()'
  )
  
  // Handle CID URLs in CSS @import or other places
  processed = processed.replace(
    /@import\s+["']cid:[^"']*["']/gi,
    ''
  )
  
  return processed
}

const replySubject = computed(() => {
  if (!selectedConversation.value) return ''
  const subject = selectedConversation.value.subject
  if (subject.startsWith('Re:') || subject.startsWith('SV:')) {
    return subject
  }
  return `Re: ${subject}`
})

const draftReplyBody = ref<string>('')

const loadEmailMappings = async () => {
  try {
    emailMappings.value = await getEmailMappings()
  } catch (error) {
    console.error('Error loading email mappings:', error)
  }
}

const loadEmailSignature = async () => {
  try {
    emailSignature.value = await getEmailSignature()
  } catch (error) {
    console.error('Error loading email signature:', error)
  }
}

const handleReplySend = async (data: { body: string; fromEmailAddress: string; cc?: string; bcc?: string }) => {
  if (!selectedConversation.value) return

  if (replyModalRef.value) {
    replyModalRef.value.setSending(true)
  }

  try {
    setStatusMessage('Sendi tölvupóst...', 'loading')
    const updatedConversation = await replyToConversation(
      selectedConversation.value.id,
      data.body,
      data.fromEmailAddress,
      data.cc,
      data.bcc
    )

    // Update the conversation with the new message
    selectedConversation.value = updatedConversation
    sortAndExpandMessages(updatedConversation)

    // Refresh conversation list
    await loadConversations()

    showReplyModal.value = false
    draftReplyBody.value = '' // Clear draft after sending
    setStatusMessage('Tölvupóstur sendur', 'success', 3000)
  } catch (error) {
    console.error('Error sending reply:', error)
    setStatusMessage('Villa kom upp við að senda tölvupóst', 'error', 5000)
  } finally {
    if (replyModalRef.value) {
      replyModalRef.value.setSending(false)
    }
  }
}

const handleReplyCancel = () => {
  showReplyModal.value = false
  draftReplyBody.value = '' // Clear draft when canceling
}

const handleWorkflowReplyClick = (draftResponse: string) => {
  draftReplyBody.value = draftResponse
  showReplyModal.value = true
}

const openJunkFilterModal = () => {
  if (selectedConversation.value) {
    junkFilterForm.value = {
      subject: selectedConversation.value.subject || '',
      senderEmail: selectedConversation.value.fromEmail || ''
    }
    junkFilterError.value = ''
    showJunkFilterModal.value = true
  }
}

const closeJunkFilterModal = () => {
  showJunkFilterModal.value = false
  junkFilterForm.value = {
    subject: '',
    senderEmail: ''
  }
  junkFilterError.value = ''
}

const saveJunkFilter = async () => {
  // Validate that at least one field is provided
  if (!junkFilterForm.value.subject?.trim() && !junkFilterForm.value.senderEmail?.trim()) {
    junkFilterError.value = 'Verður að skilgreina annaðhvort efnisgrein eða netfang sendanda'
    return
  }

  savingJunkFilter.value = true
  junkFilterError.value = ''

  try {
    await createJunkFilter({
      subject: junkFilterForm.value.subject?.trim() || null,
      senderEmail: junkFilterForm.value.senderEmail?.trim() || null,
      isActive: true
    })

    setStatusMessage('Ruslasía búin til', 'success', 3000)
    closeJunkFilterModal()
  } catch (error: any) {
    console.error('Error creating junk filter:', error)
    junkFilterError.value = error.data?.error || error.message || 'Villa kom upp við að vista ruslasíu'
  } finally {
    savingJunkFilter.value = false
  }
}

// Polling for updates
let pollingInterval: ReturnType<typeof setInterval> | null = null

const startPolling = () => {
  // Poll every 10 seconds
  pollingInterval = setInterval(() => {
    if (selectedConversationId.value) {
      // Silently refresh the conversation
      loadSelectedConversation(selectedConversationId.value, true)
    }
    // Also silently refresh the conversation list
    loadConversationsSilent()
  }, 10000)
}

const loadConversationsSilent = async () => {
  try {
    // For silent refresh, just reload the first page
    const result = await getConversations({
      status: filters.value.status || undefined,
      classification: filters.value.classification || undefined,
      page: 1,
      pageSize: pageSize.value
    })
    
    // Update existing conversations or replace if structure changed
    const existingIds = new Set(conversations.value.map(c => c.id))
    const newItems = result.items.filter(item => existingIds.has(item.id))
    
    // Update existing items
    newItems.forEach(newItem => {
      const existingIndex = conversations.value.findIndex(c => c.id === newItem.id)
      if (existingIndex !== -1) {
        conversations.value[existingIndex] = { ...conversations.value[existingIndex], ...newItem }
      }
    })
    
    totalCount.value = result.totalCount

    // If we have a selected conversation, try to find it in the new list
    if (selectedConversationId.value) {
      const found = conversations.value.find(c => c.id === selectedConversationId.value)
      if (found) {
        if (selectedConversation.value) {
          selectedConversation.value.status = found.status
          selectedConversation.value.classification = found.classification
          selectedConversation.value.messageCount = found.messageCount
          selectedConversation.value.lastMessageReceivedAt = found.lastMessageReceivedAt
        }
      }
    }
  } catch (error) {
    console.error('Error loading conversations:', error)
  }
}

const stopPolling = () => {
  if (pollingInterval) {
    clearInterval(pollingInterval)
    pollingInterval = null
  }
}

// Handle click outside to close dropdown
const handleClickOutside = (event: MouseEvent) => {
  if (actionsDropdownRef.value && !actionsDropdownRef.value.contains(event.target as Node)) {
    showActionsDropdown.value = false
  }
}

// Check for conversation ID in query params on mount
onMounted(() => {
  loadConversations()
  loadEmailMappings()
  loadEmailSignature()
  
  const conversationId = route.query.id as string | undefined
  if (conversationId) {
    selectConversation(conversationId)
  }
  
  // Start polling
  startPolling()
  
  // Add click outside handler
  document.addEventListener('click', handleClickOutside)
  
  // Update container height on mount and resize
  nextTick(() => {
    updateContainerHeight()
    window.addEventListener('resize', updateContainerHeight)
  })
})

// Watch for route changes
watch(() => route.query.id, (newId) => {
  if (newId && typeof newId === 'string' && newId !== selectedConversationId.value) {
    selectConversation(newId)
  }
})

// Cleanup polling on unmount
onUnmounted(() => {
  stopPolling()
  document.removeEventListener('click', handleClickOutside)
  window.removeEventListener('resize', updateContainerHeight)
})
</script>

