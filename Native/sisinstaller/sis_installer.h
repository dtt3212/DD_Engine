/*
 * Copyright (c) 2018 EKA2L1 Team.
 * 
 * This file is part of EKA2L1 project.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

#pragma once

#include <atomic>
#include <cstdint>
#include <functional>
#include <stack>
#include <string>
#include <vector>

#include "sis_fields.h"

namespace eka2l1 {
    namespace common {
        class ro_stream;
    }

    namespace loader {
        // An interpreter that runs SIS install script
        class sis_dd_data_installer {
            sis_controller *main_controller;
            sis_data *install_data;

            std::stack<sis_controller *> current_controllers;

            struct extract_target_info {
                std::string file_path_;
                std::uint32_t data_unit_index_;
                std::uint16_t data_unit_block_index_;
            };

            std::vector<extract_target_info> extract_targets;
            std::size_t extract_target_accumulated_size;
            std::size_t extract_target_decomped_size;

            common::ro_stream *data_stream;
            std::string destination_folder;

            bool skip_next_file{ false };

            bool appprop(const sis_uid uid, sis_property prop);
            bool package(const sis_uid uid);

            /**
             * \brief   Check if the given expression's condition can be passed.
             * \return  1 if it can be passed, -1 for error, 0 if not passed.
             */
            int condition_passed(sis_expression *expr);

            /**
             * \brief Get the true integral value from an expression.
             */
            int gasp_true_form_of_integral_expression(const sis_expression &expr);

        protected:
            bool interpret(sis_install_block &install_block, std::uint16_t crr_blck_idx = 0);
            bool interpret(sis_controller *controller, const std::uint16_t base_data_idx);

            /**
             * \brief Get the data in the index of a buffer block in the SIS.
             * 
             * The function assumes that data has small size, and will load it into a buffer.
             * Of course, if the buffer has size of something like 200 MB, it will crash.
             * 
             * This function is usually used for extracting text file.
             * 
             * \param data_idx      The index of the source buffer in block buffer.
             * \param crr_block_idx The block index.
             * 
             * \returns A vector contains binary data, uncompressed if neccessary.
             */
            std::vector<uint8_t> get_small_file_buf(uint32_t data_idx, uint16_t crr_blck_idx);

            /**
             * \brief Get the data in the index of a buffer block in the SIS, write it to a physical file.
             * 
             * Usually uses for extracting large app data.
             * 
             * \param path          UTF-8 path to the physical file.
             * \param data_idx      The index of the source buffer in block buffer.
             * \param crr_block_idx The block index..
             */
            bool extract_file(const std::string &path, const uint32_t idx, uint16_t crr_blck_idx);

        public:
            explicit sis_dd_data_installer(common::ro_stream *stream, sis_controller *main_controller, sis_data *inst_data, const std::string &destination_folder);
            bool run();
        };
    }
}