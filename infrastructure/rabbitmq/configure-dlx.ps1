
#requires -Version 7.3
[CmdletBinding()]
param(
    [string]$ContainerName = 'enterprise-rabbitmq',
    [string]$VirtualHost = '/'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandArgumentPassing = 'Standard'

$definition = '{"dead-letter-exchange":"enterprise.events.dlx","dead-letter-routing-key":"notification-service.dead"}'

# Show existing policies before changing the named policy.
& docker exec $ContainerName rabbitmqctl list_policies -p $VirtualHost

if ($LASTEXITCODE -ne 0) {
    throw 'Could not list RabbitMQ policies.'
}

& docker exec $ContainerName rabbitmqctl set_policy -p $VirtualHost `
    notification-dlx '^notification-service$' $definition `
    --apply-to queues --priority 10

if ($LASTEXITCODE -ne 0) {
    throw 'Could not apply the DLX policy.'
}

& docker exec $ContainerName rabbitmqctl list_policies -p $VirtualHost

if ($LASTEXITCODE -ne 0) {
    throw 'Could not verify RabbitMQ policies.'
}

Write-Host 'Policy configured. After API topology initialization, verify the effective queue policy and test dead-lettering.'
