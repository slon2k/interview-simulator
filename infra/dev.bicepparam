using 'main.bicep'

param baseName = 'interview-sim'
param environment = 'dev'
param skuName = 'F1'
param skuCapacity = 1
param linuxFxVersion = 'DOTNETCORE|10.0'
param speechSkuName = 'F0'
param enableAzureOpenAI = true
param openAISkuName = 'S0'
param openAIDeployModel = true
param openAIDeployments = [
	{
		name: 'gpt-5-mini'
		modelName: 'gpt-5-mini'
		modelVersion: '2025-08-07'
		deploymentSkuName: 'GlobalStandard'
		deploymentCapacity: 10
	}
	{
		name: 'gpt-5-nano'
		modelName: 'gpt-5-nano'
		modelVersion: '2025-08-07'
		deploymentSkuName: 'GlobalStandard'
		deploymentCapacity: 10
	}
	{
		name: 'gpt-5'
		modelName: 'gpt-5'
		modelVersion: '2025-08-07'
		deploymentSkuName: 'GlobalStandard'
		deploymentCapacity: 10
	}
]
