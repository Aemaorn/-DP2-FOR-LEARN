import { globalIgnores } from 'eslint/config';
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript';
import pluginVue from 'eslint-plugin-vue';
import pluginVitest from '@vitest/eslint-plugin';
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting';

// To allow more languages other than `ts` in `.vue` files, uncomment the following lines:
// import { configureVueProject } from '@vue/eslint-config-typescript'
// configureVueProject({ scriptLangs: ['ts', 'tsx'] })
// More info at https://github.com/vuejs/eslint-config-typescript/#advanced-setup

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,mts,tsx,vue}'],
  },

  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**']),

  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  {
    ...pluginVitest.configs.recommended,
    files: ['**/*.ts', '**/*.tsx', '**/*.mts', '**/*.vue'],
    rules: {
      'vue/multi-word-component-names': 'off',
      // Require return types on all functions
      // Using 'warn' for now, will enforce 'error' in the near future.
      '@typescript-eslint/explicit-function-return-type': [
        'warn',
        {
          allowExpressions: false,
          allowTypedFunctionExpressions: false,
          allowHigherOrderFunctions: false,
        },
      ],

      // Require semicolons
      semi: ['error', 'always'],
      '@typescript-eslint/semi': ['error', 'always'],

      // Require trailing commas in multiline constructs
      'comma-dangle': ['error', 'always-multiline'],

      // Disallow var
      'no-var': 'error',
      'prefer-const': 'error',

      '@typescript-eslint/no-unused-expressions': 'warn',
      '@typescript-eslint/no-explicit-any': 'warn',
      'no-extra-semi': 'error',
      'no-unexpected-multiline': 'error',
    },
  },
  skipFormatting
);
